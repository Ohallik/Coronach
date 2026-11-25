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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 7's way in as a player meets it, in the built scenes: Ishen on Many Hands' dock and the hull's warp that waits on him, and the Orrery, the machine itself (nine moons
    /// on their rails on one clock, lamps that stand amber ahead of a moon and run green behind it, rows of gates that open and shut as the moon passes them, a moon that strikes what is
    /// left on its rail, the wardens a forced gate wakes, the broken moon's shards), Ishen's talk with the machine drawn back into view, the named gates read slowly on green, the far gate
    /// and the landing, and a save and a Continue. What every zone gets from the shared checks is theirs and not repeated here. Coordinates are the map brief's (docs/maps/Orrery.md);
    /// everything about a rail is asked of its own <see cref="OrreryRail"/>, which runs the same numbers the builder laid.</summary>
    public sealed class OrielWorldTests
    {
        readonly List<string> heard=new(),log=new();readonly List<(string who,string line)> said=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))heard.Add(point.dialogueNode);}
        void Spoke(string who,string line)=>said.Add((who,line));
        static readonly string[] Heeded={"RAIL_FORCED","RAIL_KEPT","MOON_STRUCK","MOON_CALVED","GATE_READ","WALL_CONTACT","DISCOVERY"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter7");heard.Clear();log.Clear();said.Clear();
            Npc.TalkRequested+=Talked;DiscoveryPoint.DialogueRequested+=Looked;Application.logMessageReceived+=Logged;BarkService.Spoken+=Spoke;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;DiscoveryPoint.DialogueRequested-=Looked;Application.logMessageReceived-=Logged;BarkService.Spoken-=Spoke;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Oriel(ContentRegistry.Book book)
        {
            // The hull's warp to the Orrery: "No lane to Oriel yet: Ishen is waiting on the dock with his books".
            book.Lock("oriel.charted","Ishen");
            // Lume's first meeting: her stall, or later (docs/writing/Oriel.yarn).
            book.Branch("LumeFirst","shop","world");
        }

        // ------------------------------------------------------------------ the Orrery's places, as the map brief has them
        const float TalkZ=128,FarGateZ=850,LandZ=880,FromCampZ=866;
        static readonly string[] Moons={"The Steward","The Weigher","The Clerk of Roads","The Lamplighter","The Reeve","The Second Gardener","The Bellfounder","The Almoner","The Warden"};

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>The story at the Orrery's door: Oriel charted and the lane the next step, so arriving reports it.</summary>
        static void ToTheOrrery(){Flag("oriel.charted");State.questSteps["CageMakers"]=0;}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return Ready(zone);
        }
        static IEnumerator Ready(string zone)
        {
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            float deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared,"fixture: "+zone+"'s conversations never finished preparing");
            yield return new WaitForSecondsRealtime(.7f);
        }
        static IEnumerator Arrive(string zone,float most=25)
        {
            float deadline=Time.unscaledTime+most;
            while((State.zone!=zone||SceneFlow.Current.Loading)&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(zone,SceneFlow.Current.Zone,"the party never arrived in "+zone);Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return Ready(zone);
        }
        static IEnumerator SaveQuitAndContinue()
        {
            string zone=State.zone;GameServices.Current.Saves.Save("autosave",State);
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");
            title.StartGame(true);yield return null;
            yield return Arrive(zone);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static IEnumerator Settle()
        {
            float deadline=Time.unscaledTime+40;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(p=>p.flag==flag);
        static Vector3? Goal()=>Object.FindFirstObjectByType<ObjectiveHud>().Goal;
        static OrreryRail Rail(string name)=>All<OrreryRail>().Single(r=>r.railName==name);
        /// <summary>A gate's middle at the plane, and the way in toward the planet from it.</summary>
        static Vector3 Gate(OrreryRail rail,int gate){var p=rail.On(rail.gateAt[gate],rail.GateRadius);return new Vector3(p.x,1,p.y);}
        static Vector3 Inward(OrreryRail rail,Vector3 at)=>new Vector3(rail.centre.x-at.x,0,rail.centre.y-at.z).normalized;
        static Vector3 OnRail(OrreryRail rail,float along){var p=rail.On(along,rail.radius);return new Vector3(p.x,1,p.y);}
        static IEnumerator Until(System.Func<bool> condition,float most,string what)
        {
            float deadline=Time.unscaledTime+most;while(!condition()&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(condition(),"waited "+most+" s and "+what);
        }
        /// <summary>Flies the lead from six metres outside a gate in through it to six metres past: at the stick's own pace, or braking all the way (about two metres a second).</summary>
        static IEnumerator FlyThrough(OrreryRail rail,int gate,bool slowly)
        {
            var at=Gate(rail,gate);var inward=Inward(rail,at);Put(Lead,at-inward*(slowly?4:12));yield return Frames(2);
            float deadline=Time.unscaledTime+12;
            while(Vector3.Dot(Lead.transform.position-at,inward)<6&&Time.unscaledTime<deadline){Lead.motor.Move(new Vector2(inward.x,inward.z),false,slowly);yield return null;}
            Lead.motor.Move(Vector2.zero,false,true);if(Lead.motor is FlightMotor ship)ship.Halt();
            Assert.Greater(Vector3.Dot(Lead.transform.position-at,inward),5,"fixture: the lead never got through "+rail.railName+"'s gate "+gate);
        }
        static IEnumerator FlyAcross(DiscoveryPoint line)
        {
            DialogueSystem.Current.AutoAdvance=true;
            var ahead=Vector3.forward;var at=line.transform.position;at.y=1;
            Put(Lead,at-ahead*9);yield return null;
            float deadline=Time.unscaledTime+5;
            while(!DialogueSystem.Current.Running&&Vector3.Dot(Lead.transform.position-at,ahead)<9&&Time.unscaledTime<deadline){Lead.motor.Move(new Vector2(0,1),false,false);yield return null;}
            if(Lead.motor is FlightMotor ship)ship.Halt();
            yield return Settle();
        }
        static bool Amber(Color c)=>c.r>c.g*1.2f;
        static bool Green(Color c)=>c.g>c.r*1.2f;

        // ================================================================== the machine
        [UnityTest] public IEnumerator NineMoonsRideTheirRailsOnOneClockAndEachRailsLampsStandAmberAheadOfItsMoonAndRunGreenBehindIt()
        {
            ToTheOrrery();yield return Zone("Orrery");
            var rails=All<OrreryRail>().OrderByDescending(r=>r.radius).ToArray();
            CollectionAssert.AreEqual(Moons,rails.Select(r=>r.railName).ToArray(),"the Orrery's rails aren't the nine moons in Ishen's order, outermost first");
            for(int i=1;i<rails.Length;i++)
            {
                Assert.AreEqual(-rails[i-1].direction,rails[i].direction,$"{rails[i-1].railName} and {rails[i].railName} turn the same way: the rails don't counter-turn like gears");
                Assert.AreEqual(rails[0].centre,rails[i].centre,$"{rails[i].railName} isn't a ring round the same centre as the others");
            }
            var block=new MaterialPropertyBlock();
            foreach(var rail in rails)
            {
                Assert.IsNotNull(rail.moon,rail.railName+" carries no moon");
                Assert.Less(Flat(rail.moon.position,rail.MoonPoint(rail.Moon)),.5f,$"{rail.railName}'s moon isn't where its rail says it is");
                // It moves the way it goes, at its speed, along its rail.
                var before=rail.moon.position;float from=rail.Clock;yield return new WaitForSecondsRealtime(.4f);float spent=rail.Clock-from;
                var moved=rail.moon.position-before;moved.y=0;
                if(moved.magnitude<rail.speed*spent*2)Assert.AreEqual(rail.speed*spent,Vector3.Dot(moved,rail.Tangent(rail.Moon)*rail.direction),.8f,$"{rail.railName}'s moon doesn't run along its rail at its speed");
                // What its lamps show, read off the texture the strip is drawn with.
                var lamps=(Texture2D)rail.lampStrip.sharedMaterial.GetTexture("_EmissionMap");Assert.IsNotNull(lamps,rail.railName+" has no lamps lit");
                float at=rail.Moon;int Lamp(float along)=>Mathf.Clamp(Mathf.RoundToInt((along-rail.lampFrom)/rail.lampSpacing),0,rail.lampCount-1);
                float ahead=at+rail.direction*(rail.moonRadius+3),behind=at-rail.direction*(rail.moonRadius+4);
                if(Mathf.Abs(ahead)<140)Assert.IsTrue(Amber(lamps.GetPixel(Lamp(ahead),0)),$"{rail.railName}'s lamp just ahead of its moon isn't amber: {lamps.GetPixel(Lamp(ahead),0)}");
                if(Mathf.Abs(behind)<140){var c=lamps.GetPixel(Lamp(behind),0);Assert.IsFalse(Amber(c),$"{rail.railName}'s lamp just behind its moon stands amber");}
                // Every gate shows what the rule says of it, now.
                for(int g=0;g<3;g++)
                {
                    rail.gateLamps[g].GetPropertyBlock(block);var shown=block.GetColor("_EmissionColor");bool shut=!rail.Open(g);
                    Assert.IsTrue(shut?Amber(shown):!Amber(shown),$"{rail.railName}'s gate {g} is {(shut?"shut":"open")} and shows {shown}");
                }
            }
            Assert.AreEqual(1,All<OrreryRail>().Select(r=>OrreryRail.Period).Distinct().Count());
        }
        /// <summary>D417 (nov24-whole s38/081, 101, 104, 107): a pale bar crossed every gate at its ring's middle, nearly full width, in three pieces (the generated ring's own bar,
        /// in its map's pale sky patches). Nothing of a lane gate is drawn across its opening now, from 2 m under its ring's middle to 2 m over it. A bar as the capture drew it,
        /// out past the rim each side, laid at the Steward's middle gate, is read first and must be found.</summary>
        [UnityTest] public IEnumerator NothingCrossesAGatesOpeningAtItsMiddle()
        {
            ToTheOrrery();yield return Zone("Orrery");
            const float RingDrop=2.6f;   // OrielBuilder.Orrery.RingDrop: a ring's middle under the plane
            var rails=All<OrreryRail>();Assert.AreEqual(Moons.Length,rails.Length,"fixture: the rails");
            // Triangles whose middles lie across gate g's opening: within its half width less .8 m of its middle, 2 m of its ring's middle in height and 8 m of its row.
            int Across(OrreryRail rail,int g,Mesh mesh,Matrix4x4 world)
            {
                float row=rail.radius+rail.gateOut,a=rail.gateAt[g]/row,middle=rail.plane-RingDrop;var at=rail.On(rail.gateAt[g],row);
                var along=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var outward=new Vector2(Mathf.Sin(a),-Mathf.Cos(a));var v=mesh.vertices;var t=mesh.triangles;int n=0;
                for(int i=0;i+2<t.Length;i+=3)
                {
                    var c=world.MultiplyPoint3x4((v[t[i]]+v[t[i+1]]+v[t[i+2]])/3);var d=new Vector2(c.x-at.x,c.z-at.y);
                    if(Mathf.Abs(Vector2.Dot(d,along))<rail.gateHalf-.8f&&Mathf.Abs(c.y-middle)<2&&Mathf.Abs(Vector2.Dot(d,outward))<8)n++;
                }
                return n;
            }
            {
                var steward=rails.Single(r=>r.railName=="The Steward");float row=steward.radius+steward.gateOut,a=steward.gateAt[1]/row;var at=steward.On(steward.gateAt[1],row);
                var bar=new Mesh{name="the capture's bar"};bar.SetVertices(new List<Vector3>{new(-.5f,-.5f,0),new(.5f,-.5f,0),new(.5f,.5f,0),new(-.5f,.5f,0)});bar.SetTriangles(new[]{0,1,2,0,2,3},0);
                var world=Matrix4x4.TRS(new Vector3(at.x,steward.plane-RingDrop,at.y),Quaternion.LookRotation(new Vector3(Mathf.Sin(a),0,-Mathf.Cos(a))),new Vector3(steward.gateHalf*2+6,1,1));
                int found=Across(steward,1,bar,world);Object.Destroy(bar);
                Assert.Greater(found,0,"fixture: a bar across the Steward's middle gate isn't found");
            }
            var faults=new List<string>();int gates=0;
            foreach(var rail in rails)for(int g=0;g<3;g++)
            {
                var piece=rail.transform.Find(rail.railName+" gate "+(g+1));Assert.IsNotNull(piece,$"fixture: {rail.railName}'s gate {g+1}");gates++;
                foreach(var f in piece.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(f.sharedMesh==null||f.name=="Arch")continue;Assert.IsTrue(f.sharedMesh.isReadable,"fixture: "+f.name+"'s mesh can be read");
                    int n=Across(rail,g,f.sharedMesh,f.transform.localToWorldMatrix);if(n>0)faults.Add($"{rail.railName}'s gate {g+1}: {n} triangles of {f.sharedMesh.name} across its opening at its middle");
                }
            }
            Assert.AreEqual(Moons.Length*3,gates,"fixture: three lane gates on every rail");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>D417 (nov24-whole s38/110): as the landing's prompt came up, the lead's nose was drawn into the beacon's base. A ship where the route asks to land (5 m short of
        /// the beacon's place, frames.csv z 875) has its nose 3 m ahead; from the play camera there (17.6 m back, 19.45 m over the plane, frames.csv) the nose hides what lies under
        /// it out to where its sight line meets the beacon's foot. The beacon's nearest edge stands a metre past that, and the prompt's place is where it was. The capture's beacon
        /// (centred on the prompt's place) is read first and must fail.</summary>
        [UnityTest] public IEnumerator TheLandingBeaconStandsClearOfAShipAtItsPrompt()
        {
            ToTheOrrery();yield return Zone("Orrery");
            var land=All<WarpBeacon>().Single(w=>w.prompt=="Land — The Skyfields");var at=land.transform.position;
            Assert.AreEqual(880,at.z,.01f,"the prompt's place moved");Assert.AreEqual(0,at.x,.01f,"the prompt's place moved");
            var drawn=land.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((x,y)=>{x.Encapsulate(y);return x;});
            var nose=new Vector3(at.x,at.y+.4f,at.z-5+3);var eye=new Vector3(at.x,at.y+19.45f,at.z-5-17.6f);
            float Hidden(float foot)=>nose.z+(nose.y-foot)*(nose.z-eye.z)/(eye.y-nose.y);
            bool Clear(Bounds b)=>b.min.z>=Hidden(b.min.y)+1;
            var old=drawn;old.center=new Vector3(at.x,drawn.center.y,at.z);
            Assert.IsFalse(Clear(old),$"fixture: the capture's beacon ({old.min.z:0.0} to {old.max.z:0.0}) reads clear of a nose that hides the ground to {Hidden(old.min.y):0.0}");
            Assert.IsTrue(Clear(drawn),$"the beacon's nearest edge is at z {drawn.min.z:0.0}: a ship at its prompt hides the ground to {Hidden(drawn.min.y):0.0}, so its nose is drawn into the beacon");
        }
        /// <summary>D399 (nov23-whole s38/081 at the top of the frame, 090 to 108 through the ship): each rail's wardens' track carried one lit strip its whole 84 m, white lit by the
        /// 1.3 sun over a white emission, and the bloom drew it as a white line across the frame. Its light is lamps now: no lit piece of it longer than a metre, a row of them,
        /// and at the most the Orrery's sun and sky give a lens only one channel over the zone's bloom threshold (.85, OrielBuilder.Looks), warm, so it blooms gold and not white.
        /// The capture's track (its 84 m strip and its colours, laid on the Steward's rail's line) is read first and must fail both ways.</summary>
        [UnityTest] public IEnumerator EachWardensTrackIsARowOfWarmLampsNotAWhiteLineAcrossTheFrame()
        {
            ToTheOrrery();yield return Zone("Orrery");
            var sun=All<Light>().Where(l=>l.type==LightType.Directional&&l.isActiveAndEnabled).Select(l=>l.intensity).DefaultIfEmpty(1).Max();
            var sky=RenderSettings.ambientSkyColor;float most=sun+Mathf.Max(sky.r,sky.g,sky.b);
            var rails=All<OrreryRail>();int tracks=0;
            {
                // The old state: the rod and one strip the track's length, .08 by .06, base (.92, .94, 1) and emission (1.1, 1.15, 1.3).
                var rail=rails.Single(r=>r.railName=="The Steward");var (from,to)=TrackEnds(rail);
                var way=(to-from).normalized;var side=Vector3.Cross(Vector3.up,way)*.04f;var up=Vector3.up*.03f;var mid=(from+to)*.5f+Vector3.down*.92f;var half=(to-from)*.5f;
                var strip=new List<Vector3>();foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})foreach(int c in new[]{-1,1})strip.Add(mid+half*a+side*b+up*c);
                var mesh=new Mesh{name="old warden track"};mesh.SetVertices(new List<Vector3>{from,to,from+Vector3.down}.Concat(strip).ToList());mesh.subMeshCount=2;
                mesh.SetTriangles(new[]{0,1,2},0);mesh.SetTriangles(new[]{3,4,5,4,6,5,7,9,8,8,9,10,3,7,4,4,7,8,5,6,9,6,10,9},1);
                var bronze=rail.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name==rail.railName+" warden track").sharedMaterials[0];
                var white=new Material(bronze);white.SetColor("_BaseColor",new Color(.92f,.94f,1));white.SetColor("_EmissionColor",new Color(1.1f,1.15f,1.3f));
                var old=TrackProblems("the capture's "+rail.railName,mesh,Matrix4x4.identity,new[]{bronze,white},way,most);
                Object.Destroy(mesh);Object.Destroy(white);
                Assert.IsTrue(old.Any(p=>p.Contains("a line"))&&old.Any(p=>p.Contains("bloom")),"fixture: the capture's track isn't read as a white line: "+string.Join("; ",old));
            }
            var problems=new List<string>();
            foreach(var rail in rails)
            {
                var drawn=rail.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name==rail.railName+" warden track").ToArray();
                Assert.AreEqual(1,drawn.Length,rail.railName+" hasn't one drawn wardens' track");
                var view=drawn[0];var (from,to)=TrackEnds(rail);
                problems.AddRange(TrackProblems(rail.railName,view.GetComponent<MeshFilter>().sharedMesh,view.transform.localToWorldMatrix,view.sharedMaterials,(to-from).normalized,most));
                tracks++;
            }
            Assert.AreEqual(Moons.Length,tracks,"not every rail has a wardens' track");
            Assert.IsEmpty(problems,string.Join("\n",problems));
        }
        /// <summary>A wardens' track's ends on the plane, as the builder lays it: 8.5 m inside the rail, from 6 m past one lane wall to 6 m past the other.</summary>
        static (Vector3 from,Vector3 to) TrackEnds(OrreryRail rail)
        {
            Vector3 At(float along){var p=rail.On(along,rail.radius-8.5f);return new Vector3(p.x,rail.plane,p.y);}
            return (At(-rail.laneHalf-6),At(rail.laneHalf+6));
        }
        /// <summary>What is wrong with a wardens' track's light: each material that shines (emission over .3; the bronze's own faint light is not a lamp) must at the most the sun and
        /// sky give it have only one channel over the bloom's .85, be warm, and be drawn in pieces under a metre long, twenty or more of them.</summary>
        static List<string> TrackProblems(string name,Mesh mesh,Matrix4x4 world,Material[] materials,Vector3 along,float most)
        {
            const float Bloom=.85f;var problems=new List<string>();var vertices=mesh.vertices;int lit=0;
            for(int sub=0;sub<mesh.subMeshCount&&sub<materials.Length;sub++)
            {
                var material=materials[sub];if(material==null||!material.HasProperty("_EmissionColor"))continue;
                var glow=material.GetColor("_EmissionColor");if(Mathf.Max(glow.r,glow.g,glow.b)<.3f)continue;
                lit++;
                // How bright a lens can be: its albedo under the sun and the sky at their fullest, and its own light.
                var albedo=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):Color.white;
                var brightest=new Color(albedo.r*most+glow.r,albedo.g*most+glow.g,albedo.b*most+glow.b);
                var channels=new[]{brightest.r,brightest.g,brightest.b}.OrderByDescending(c=>c).ToArray();
                if(channels[1]>=Bloom)problems.Add($"{name}'s track lamps are {brightest} at the most the sun and sky give them: two channels over the bloom's {Bloom}, so they bloom toward white");
                if(!(brightest.r>=brightest.g&&brightest.g>=brightest.b))problems.Add($"{name}'s track lamps are {brightest}: not the Orrery's warm light");
                // Its lit pieces along the track: each lit triangle covers a stretch of it, and stretches that overlap or come within half a metre of each other are one piece.
                var triangles=mesh.GetTriangles(sub);float Along(int i)=>Vector3.Dot(world.MultiplyPoint3x4(vertices[i]),along);
                var spans=new List<(float from,float to)>();
                for(int t=0;t+2<triangles.Length;t+=3){float a=Along(triangles[t]),b=Along(triangles[t+1]),c=Along(triangles[t+2]);spans.Add((Mathf.Min(a,Mathf.Min(b,c)),Mathf.Max(a,Mathf.Max(b,c))));}
                if(spans.Count==0){problems.Add(name+"'s track has a lit material and nothing drawn with it");continue;}
                spans.Sort((x,y)=>x.from.CompareTo(y.from));
                var pieces=new List<float>();float start=spans[0].from,end=spans[0].to;
                foreach(var (from,to) in spans.Skip(1)){if(from-end>.5f){pieces.Add(end-start);start=from;}end=Mathf.Max(end,to);}
                pieces.Add(end-start);
                if(pieces.Max()>=1)problems.Add($"{name}'s track has a lit piece {pieces.Max():0.0} m long: a line, not lamps");
                if(pieces.Count<20)problems.Add($"{name}'s track has {pieces.Count} lamps across the lane: too few to read as a lit rail");
            }
            if(lit!=1)problems.Add($"{name}'s track has {lit} lit materials: its lamps are one");
            return problems;
        }

        [UnityTest] public IEnumerator AGateTheMoonIsComingToIsShutAndAGateItHasPassedIsOpenSoTheRowGoesAmberInTurnAndGreenInTheSameOrder()
        {
            ToTheOrrery();yield return Zone("Orrery");
            foreach(var rail in All<OrreryRail>())
            {
                // From a moment when all three of the lane's gates stand open, when each next shuts and then opens again.
                float from=0;while(from<OrreryRail.Period&&!(Enumerable.Range(0,3).All(g=>!rail.GateShut(g,rail.MoonAt(from)))))from+=.02f;
                Assert.Less(from,OrreryRail.Period,rail.railName+"'s three gates are never all open at once");
                var shuts=new float[3];var opens=new float[3];
                for(int g=0;g<3;g++){shuts[g]=opens[g]=-1;
                    for(float t=from;t<from+OrreryRail.Period*2;t+=.02f){bool shut=rail.GateShut(g,rail.MoonAt(t));if(shut&&shuts[g]<0)shuts[g]=t;if(!shut&&shuts[g]>=0&&opens[g]<0)opens[g]=t;}}
                // The order the moon comes to them in: east first for a moon running west.
                var order=rail.direction<0?new[]{2,1,0}:new[]{0,1,2};
                for(int k=1;k<3;k++)
                {
                    Assert.Less(shuts[order[k-1]],shuts[order[k]],$"{rail.railName}: gate {order[k]} shuts before gate {order[k-1]}, which the moon comes to first");
                    Assert.Less(opens[order[k-1]],opens[order[k]],$"{rail.railName}: gate {order[k]} opens before gate {order[k-1]}, which the moon passes first");
                }
                // A gate is shut while the moon is in its amber reach and open once the moon is a hull past it: the lee.
                float m=rail.GateAlongRail(1);
                Assert.IsTrue(rail.GateShut(1,m-rail.direction*(rail.moonRadius+1)),rail.railName+"'s middle gate is open with the moon about to sweep it");
                Assert.IsFalse(rail.GateShut(1,m+rail.direction*(rail.moonRadius+rail.gateHalf+OrreryRail.HullClear+.5f)),rail.railName+"'s middle gate is shut with the moon past it");
                // Shut for a few seconds a turn, and open the rest: something to wait out, not something to fly round.
                float shutFor=opens[1]-shuts[1];
                Assert.That(shutFor,Is.InRange(2.5f,7f),$"{rail.railName}'s middle gate is shut {shutFor:0.0} s a turn");
            }
        }

        [UnityTest] public IEnumerator AShipThatTakesAGreenGateWakesNothingAndOneThatForcesAnAmberGateWakesItsRailsWardens()
        {
            ToTheOrrery();Flag("oriel.lightsRead");yield return Zone("Orrery");
            var weigher=Rail("The Weigher");var watch=All<EncounterVolume>().Single(e=>e.encounterId=="Rail_2");
            Assert.IsNotNull(watch.previewPoint,"the Weigher's wardens aren't on show before they wake");
            foreach(var spawner in watch.spawners){Assert.IsTrue(spawner.faceAsPlaced,"a warden isn't set facing along its rail");Assert.Greater(Mathf.Abs(Vector3.Dot(spawner.transform.forward,weigher.Tangent(0))),.95f,"a warden doesn't face along its rail");}
            // On green: through the middle gate with the moon well away.
            yield return Until(()=>weigher.Open(1)&&weigher.UntilShut(1,weigher.Moon)>4,25,"the Weigher's middle gate never stood open for four seconds");
            yield return FlyThrough(weigher,1,false);
            Assert.IsTrue(log.Any(l=>l.StartsWith("RAIL_KEPT The Weigher 1")),"going through an open gate wasn't seen: "+string.Join(" | ",log));
            Assert.IsFalse(watch.Started,"going through an open gate woke the Weigher's wardens");
            // On amber: the Lamplighter's middle gate with its moon coming to it.
            var lamplighter=Rail("The Lamplighter");var wake=All<EncounterVolume>().Single(e=>e.encounterId=="Rail_4");
            yield return Until(()=>!lamplighter.Open(1)&&lamplighter.AheadOfMoon(lamplighter.GateAlongRail(1),lamplighter.Moon)>lamplighter.moonRadius+lamplighter.gateHalf+2,25,"the Lamplighter's moon never came toward its middle gate");
            yield return FlyThrough(lamplighter,1,false);
            Assert.IsTrue(log.Any(l=>l.StartsWith("RAIL_FORCED The Lamplighter 1")),"going through a shut gate wasn't seen as forcing it: "+string.Join(" | ",log));
            Assert.IsTrue(wake.Started,"forcing the Lamplighter's gate didn't wake its wardens");
            string forced=BarkService.Line(Lead.character,"rail.forced");Assert.IsTrue(said.Any(s=>s.who==Lead.character&&s.line==forced),"nobody said the rails are waking");
            Assert.IsFalse(watch.Started,"forcing one rail woke another's wardens");
        }

        [UnityTest] public IEnumerator AMoonStrikesAShipLeftOnItsRailThrowsItClearAndLeavesAShipInItsLeeAlone()
        {
            ToTheOrrery();Flag("oriel.lightsRead");yield return Zone("Orrery");
            var steward=Rail("The Steward");var lead=Lead;var partner=Partner;
            // The moon coming in from the east toward the lane's middle, its marking already down.
            yield return Until(()=>{float a=steward.AheadOfMoon(0,steward.Moon);return a>steward.moonRadius+7&&a<steward.moonRadius+11;},25,"the Steward's moon never came toward the lane's middle");
            lead.Health.InvulnerableUntil=0;lead.Health.integrity=lead.Health.maximum;Put(lead,OnRail(steward,0));
            float before=lead.Health.integrity;
            // The partner tucked in behind the moon: in its lee, on the rail it has just swept.
            partner.Health.InvulnerableUntil=0;partner.Health.integrity=partner.Health.maximum;float partnerBefore=partner.Health.integrity;
            Put(partner,OnRail(steward,steward.Moon-steward.direction*(steward.moonRadius+2.5f)));
            yield return Until(()=>log.Any(l=>l.StartsWith("MOON_STRUCK "+lead.character)),6,"the Steward's moon never struck a ship sitting on its rail");
            float lost=(before-lead.Health.integrity)/lead.Health.maximum,expected=steward.strike*100/(100+Mathf.Max(0,lead.Plating-10));
            Assert.AreEqual(expected,lost,.015f,$"a moon's blow took {lost:P1} of the hull through Plating {lead.Plating}: a fifth of it before plating is {expected:P1}");
            Assert.Greater(lost,.09f,$"a moon's blow took only {lost:P1}: no heavier than an ordinary enemy's hit");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(Mathf.Abs(steward.FromCentre(lead.transform.position)-steward.radius),steward.moonRadius,"the struck ship was left on the moon's rail");
            Assert.AreEqual(partnerBefore,partner.Health.integrity,"a ship in the moon's lee was struck");
            Assert.IsFalse(log.Any(l=>l.StartsWith("MOON_STRUCK "+partner.character)),"the moon struck the ship behind it");
            // Never the last of it: the next moon finds a hull with almost nothing left, and leaves it something.
            var weigher=Rail("The Weigher");
            yield return Until(()=>{float a=weigher.AheadOfMoon(0,weigher.Moon);return a>weigher.moonRadius+7&&a<weigher.moonRadius+11;},25,"the Weigher's moon never came toward the lane's middle");
            lead.Health.integrity=lead.Health.maximum*.05f;lead.Health.InvulnerableUntil=0;Put(lead,OnRail(weigher,0));
            yield return Until(()=>log.Count(l=>l.StartsWith("MOON_STRUCK "+lead.character))>=2,6,"the Weigher's moon never struck the ship on its rail");
            Assert.Greater(lead.Health.integrity,0,"a moon took the last of a hull");
        }

        [UnityTest] public IEnumerator TheBrokenMoonCalvesAShardOffItsLeadingFaceAsItComesIntoTheLane()
        {
            ToTheOrrery();Flag("oriel.lightsRead");yield return Zone("Orrery");
            var clerk=Rail("The Clerk of Roads");Assert.AreEqual(3,clerk.shards.Length,"the broken moon has no shards to shed");
            Put(Lead,new Vector3(0,1,clerk.centre.y-clerk.radius-30));
            yield return Until(()=>log.Any(l=>l.StartsWith("MOON_CALVED The Clerk of Roads")),OrreryRail.Period+3,"the broken moon came through the lane and shed nothing");
            var calf=clerk.shards.First(s=>s.Started);var at=calf.spawners[0].transform.position;
            float ahead=Vector3.Dot(at-clerk.moon.position,clerk.Tangent(clerk.Moon)*clerk.direction);
            Assert.Greater(ahead,clerk.moonRadius,"the shard came off somewhere other than the moon's leading face");
            Assert.Greater(Vector3.Dot(calf.spawners[0].transform.forward,clerk.Tangent(clerk.Moon)*clerk.direction),.9f,"the shard doesn't face the way the moon is going");
            string shed=BarkService.Line(Lead.character,"moonshard");Assert.IsTrue(said.Any(s=>s.who==Lead.character&&s.line==shed),"nobody said the moon is shedding");
            Assert.IsFalse(All<OrreryRail>().Where(r=>r!=clerk).Any(r=>r.shards.Length>0),"a whole moon sheds");
        }

        // ================================================================== reading it
        [UnityTest] public IEnumerator LumeHailsOnceIshenNamesTheMoonsOnceAndTheViewDrawsBackOverTheFirstRailsWhileHeDoes()
        {
            ToTheOrrery();yield return Zone("Orrery");
            Assert.AreEqual(1,Step("CageMakers"),"arriving in the Orrery didn't move the story on to the crossing");
            Put(Lead,new Vector3(0,1,18));
            yield return Until(()=>Has("oriel.podHailed"),5,"Lume says nothing as the party sets off");
            yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(said.Any(s=>s.who=="Lume"),"Lume's hail was heard and nothing was said");
            var pod=All<CourierPod>().SingleOrDefault();Assert.IsNotNull(pod,"there is no courier pod behind the party");
            // Up to the talk: the view draws back to take in the first rail's gates (over the second rail too, the ship was a speck: D359, ChapterFourToSixLookTests holds it
            // at sixty pixels or more).
            var view=All<VantageView>().Single();var camera=Camera.main;
            Put(Lead,new Vector3(0,1,TalkZ-44));yield return new WaitForSecondsRealtime(2.5f);
            Assert.IsTrue(view.Holding,"coming up to Ishen's talk, the view doesn't draw back over the machine");
            float back=Vector3.Distance(camera.transform.position,Lead.transform.position);
            Assert.Greater(back,60,$"the camera is only {back:0} m from the ship: the first rail's gates aren't in view");
            var steward=Rail("The Steward");
            foreach(var gate in new[]{0,1,2})
            {var seen=camera.WorldToViewportPoint(Gate(steward,gate));Assert.IsTrue(seen.z>0&&seen.x>0&&seen.x<1&&seen.y>0&&seen.y<1,$"the Steward's gate {gate} isn't in the frame while Ishen names the moons");}
            var talk=Point("oriel.lightsRead");Assert.AreEqual("IshenMoonNames",talk.dialogueNode);Assert.IsTrue(talk.startsOnCrossing,"Ishen's talk can be flown round");
            Assert.Less(Mathf.Abs(talk.transform.position.z-TalkZ),1,"Ishen's talk isn't short of the first gates where the map has it");
            yield return FlyAcross(talk);
            Assert.AreEqual(1,heard.Count(h=>h=="IshenMoonNames"),"Ishen's talk didn't play as the ship crossed its line: "+string.Join(", ",heard));
            yield return Frames(3);Assert.IsFalse(view.Holding,"the view stays drawn back once the lights are taught");
            yield return FlyAcross(talk);Assert.AreEqual(1,heard.Count(h=>h=="IshenMoonNames"),"Ishen named the moons twice");
            // His three later names come one at each later rail, in order, and only after the talk.
            var calls=All<RadioHail>().Where(r=>r.speaker=="Ishen").OrderBy(r=>r.transform.position.z).ToArray();
            Assert.AreEqual(3,calls.Length,"Ishen doesn't name the seventh, eighth and ninth moons");
            Assert.Contains("oriel.lightsRead",calls[0].needs,"Ishen names the seventh moon before he has named the first six");
            for(int k=1;k<3;k++)Assert.Contains(calls[k-1].heardFlag,calls[k].needs,"Ishen's later names can come out of order");
        }

        [UnityTest] public IEnumerator TheNamedGatesAreReadInIshensOrderOnlyWhenPassedOnGreenAndSlowly()
        {
            ToTheOrrery();Flag("oriel.lightsRead");Flag("oriel.ruleSeen");State.questSteps["MoonNames"]=0;yield return Zone("Orrery");
            var steward=Rail("The Steward");var weigher=Rail("The Weigher");
            var read=new[]{Point("oriel.stewardRead"),Point("oriel.weigherRead"),Point("oriel.lamplighterRead")};
            Assert.IsTrue(read.All(p=>p.range<=0),"a keystone can be read with a button: passing is the only way");
            Assert.IsTrue(read[0].Wanted,"the Steward's keystone isn't wanted with the quest asking");
            Assert.IsFalse(read[1].Wanted||read[2].Wanted,"a later keystone is wanted before the Steward's is read");
            // A side step is the panel's dim second line ("Also: ..."), whose arrow goes to the nearest point the step still wants: only the Steward's is wanted yet.
            yield return new WaitForSecondsRealtime(.6f);var panel=Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t=>t.text).FirstOrDefault(t=>t.Contains("Also:"))??"";
            StringAssert.Contains("Pass the named gates",panel,"the objective panel says nothing of the names with the quest asking for them");
            Assert.AreEqual(1,All<DiscoveryPoint>().Count(p=>p.report=="NamedGate"&&p.Wanted),"more than one keystone is wanted at once: the arrow could send the party to a later one first");
            // Fast, on green: not read.
            yield return Until(()=>steward.Open(1)&&steward.UntilShut(1,steward.Moon)>5,25,"the Steward's middle gate never stood open");
            yield return FlyThrough(steward,1,false);Assert.IsFalse(Has("oriel.stewardRead"),"a gate flown through at full pace was read");
            // Slowly, on green: read, and Ishen says so.
            yield return Until(()=>steward.Open(1)&&steward.UntilShut(1,steward.Moon)>8,25,"the Steward's middle gate never stood open long enough to pass it slowly");
            yield return FlyThrough(steward,1,true);
            Assert.IsTrue(Has("oriel.stewardRead"),"passing the Steward's gate slowly on green didn't read it: "+string.Join(" | ",log));
            Assert.AreEqual(1,State.questCounts.TryGetValue("MoonNames/0",out int count)?count:0,"reading the Steward's gate didn't count toward the names");
            Assert.IsTrue(said.Any(s=>s.who=="Ishen"),"Ishen says nothing over the radio as the Steward's gate is read");
            Assert.IsTrue(read[1].Wanted,"the Weigher's keystone isn't wanted once the Steward's is read");
            // Slowly, on amber: not read.
            yield return Until(()=>!weigher.Open(1)&&weigher.AheadOfMoon(weigher.GateAlongRail(1),weigher.Moon)>weigher.moonRadius+weigher.gateHalf+6,25,"the Weigher's moon never came toward its middle gate");
            yield return FlyThrough(weigher,1,true);
            Assert.IsFalse(Has("oriel.weigherRead"),"a gate passed while it was shut was read");
        }

        // ================================================================== the way through
        [UnityTest] public IEnumerator EveryGateTakesTwoHullsIdrasIncludedAndAHullFlownThroughAGatesMiddleTouchesNothing()
        {
            ToTheOrrery();Flag("oriel.lightsRead");yield return Zone("Orrery");
            float need=HeroCollision.HullRadius("Taren")+HeroCollision.Apart("Taren","Idra")+HeroCollision.HullRadius("Idra");
            foreach(var rail in All<OrreryRail>())
            {
                for(int g=0;g<3;g++)
                {
                    var at=Gate(rail,g);var along=rail.Tangent(rail.gateAt[g]*rail.radius/rail.GateRadius);
                    float Clear(Vector3 way)=>Physics.Raycast(at,way,out var hit,20,~0,QueryTriggerInteraction.Ignore)?hit.distance:20;
                    float wide=Clear(along)+Clear(-along);
                    Assert.GreaterOrEqual(wide,need,$"{rail.railName}'s gate {g} is {wide:0.0} m across at the plane: two hulls side by side need {need:0.0}");
                    Assert.IsFalse(Physics.Raycast(at-Inward(rail,at)*8,Inward(rail,at),16,~0,QueryTriggerInteraction.Ignore),$"something solid stands in {rail.railName}'s gate {g}");
                }
                // And the solid between the gates is there: nothing gets across a row but through a gate.
                var between=rail.On((rail.gateAt[0]+rail.gateAt[1])*.5f,rail.GateRadius);var from=new Vector3(between.x,1,between.y);
                Assert.IsTrue(Physics.Raycast(from-Inward(rail,from)*6,Inward(rail,from),12,~0,QueryTriggerInteraction.Ignore),$"a ship can cross {rail.railName}'s gate row between its gates");
            }
            var rails=All<OrreryRail>().OrderByDescending(r=>r.radius).ToArray();
            for(int i=1;i<rails.Length;i++)
            {
                float gap=(rails[i-1].radius-rails[i-1].moonRadius)-(rails[i].GateRadius+.8f);
                Assert.GreaterOrEqual(gap,4*HeroCollision.HullRadius("Idra"),$"between {rails[i-1].railName}'s moon and {rails[i].railName}'s gates there are only {gap:0.0} m to wait in");
            }
            // Idra's hull, the widest, flown through the Steward's three gates on green, touches nothing.
            int tries=0;while(Lead.character!="Idra"&&tries++<3){PartyController.Current.Swap(true);yield return Frames(3);}
            Assert.AreEqual("Idra",Lead.character,"fixture: Idra isn't out to fly");
            var steward=Rail("The Steward");
            for(int g=0;g<3;g++)
            {
                int gate=g;yield return Until(()=>steward.Open(gate)&&steward.UntilShut(gate,steward.Moon)>4,25,"the Steward's gate "+gate+" never stood open");
                yield return FlyThrough(steward,gate,false);
            }
            Assert.IsFalse(log.Any(l=>l.StartsWith("WALL_CONTACT")&&l.Contains("Idra")),"Idra's hull touched a gate: "+string.Join(" | ",log.Where(l=>l.StartsWith("WALL_CONTACT"))));
        }

        [UnityTest] public IEnumerator TheFarGateCrossesTheOrreryAndTheLandingSetsThePartyDownAtIshensCamp()
        {
            ToTheOrrery();Flag("oriel.lightsRead");yield return Zone("Orrery");
            Assert.AreEqual(1,Step("CageMakers"),"arriving in the Orrery didn't move the story on to the crossing");
            var far=Point("oriel.crossed");Assert.AreEqual("FarGate",far.report);Assert.IsTrue(far.startsOnCrossing,"the far gate can be flown round");
            Assert.Less(Mathf.Abs(far.transform.position.z-FarGateZ),1,"the far gate isn't where the map has it");
            yield return Frames(3);var goal=Goal();Assert.IsTrue(goal.HasValue&&Flat(goal.Value,new Vector3(0,0,FarGateZ))<3,$"the objective points at {goal}, not at the far gate");
            yield return FlyAcross(far);
            Assert.IsTrue(Has("oriel.crossed"),"flying past the far gate didn't cross the Orrery");Assert.AreEqual(2,Step("CageMakers"),"crossing the Orrery didn't move the story on to the landing");
            var land=All<WarpBeacon>().Single(w=>w.scene=="OrielSkyfields");Assert.Less(Flat(land.transform.position,new Vector3(0,1,LandZ)),2,"the landing isn't at the lane's end");
            Assert.AreEqual("Arrival",land.spawn,"the landing doesn't set down at Ishen's camp");
            var home=All<WarpBeacon>().Single(w=>w.scene=="ManyHandsHull");Assert.AreEqual("Oriel",home.spawn,"the warp home doesn't come out by the Orrery's own warp");
            Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id=="OrielSkyfields"&&Flat(s.transform.position,new Vector3(0,1,FromCampZ))<2),"there is nowhere for a ship coming back up from the camp to arrive");
            if(Application.CanStreamedLevelBeLoaded("OrielSkyfields"))
            {
                Put(Lead,land.transform.position+Vector3.back*2);yield return Frames(3);land.Interact();yield return Arrive("OrielSkyfields");
                Assert.AreEqual(2,Step("CageMakers"),"landing moved the story past Ishen's camp by itself");
            }
        }

        [UnityTest] public IEnumerator SavingInTheOrreryAndContinuingKeepsTheTalkTheReadGateAndTheCrossing()
        {
            ToTheOrrery();Flag("oriel.lightsRead");Flag("oriel.podHailed");State.questSteps["MoonNames"]=0;yield return Zone("Orrery");
            var steward=Rail("The Steward");
            yield return Until(()=>steward.Open(1)&&steward.UntilShut(1,steward.Moon)>8,25,"the Steward's middle gate never stood open");
            yield return FlyThrough(steward,1,true);Assert.IsTrue(Has("oriel.stewardRead"),"fixture: the Steward's gate wasn't read");
            yield return FlyAcross(Point("oriel.crossed"));
            Put(Lead,new Vector3(0,1,600));yield return Frames(2);
            yield return SaveQuitAndContinue();
            Assert.AreEqual("Orrery",State.zone);
            foreach(string f in new[]{"oriel.lightsRead","oriel.stewardRead","oriel.crossed"})Assert.IsTrue(Has(f),f+" was lost to a save");
            Assert.IsFalse(Point("oriel.lightsRead").Available,"Ishen's talk is waiting again after a Continue");Assert.IsFalse(Point("oriel.stewardRead").Wanted,"the Steward's gate is to read again after a Continue");
            Assert.IsTrue(Point("oriel.weigherRead").Wanted,"after a Continue the Weigher's gate isn't the next to read");
            Assert.AreEqual(2,Step("CageMakers"),"the story's place was lost to a save");
        }

        [UnityTest] public IEnumerator TheCourierPodTrailsTheRearShipAndNeverComesNearAHull()
        {
            ToTheOrrery();Flag("oriel.lightsRead");yield return Zone("Orrery");
            var pod=All<CourierPod>().Single();Assert.IsEmpty(pod.GetComponentsInChildren<Collider>(),"the courier pod can be flown into");
            var partner=Partner;partner.GetComponent<PartnerBrain>().enabled=true;
            Put(Lead,new Vector3(0,1,20));Put(partner,new Vector3(3,1,16));yield return Frames(3);
            float nearest=float.MaxValue,deadline=Time.unscaledTime+5;
            while(Time.unscaledTime<deadline){Lead.motor.Move(new Vector2(0,1),false,false);foreach(var hero in PartyController.Current.members)nearest=Mathf.Min(nearest,Vector3.Distance(hero.transform.position,pod.transform.position));yield return null;}
            Lead.motor.Move(Vector2.zero,false,true);yield return new WaitForSecondsRealtime(2.5f);
            var rear=PartyController.Current.members.OrderBy(m=>m.transform.position.z).First();
            Assert.Less(pod.transform.position.z,rear.transform.position.z,"the courier pod isn't behind the party");
            Assert.Less(Flat(pod.transform.position,rear.transform.position),14,"the courier pod has fallen far behind the party");
            Assert.Greater(nearest,2.5f,$"the courier pod came within {nearest:0.0} m of a hull");
            Flag("chapter7.complete");yield return Frames(2);Assert.IsFalse(pod.gameObject.activeSelf,"the courier pod still flies once the chapter is done");
        }

        // ================================================================== the way in
        [UnityTest] public IEnumerator IshenArrivesOnTheDockOnceIdrasCuttersAreHomeAndTheHullsWarpToTheOrreryWaitsForHim()
        {
            Assert.IsTrue(Has("chapter6.complete"),"fixture: chapter 7's loadout hasn't finished chapter 6");Assert.AreEqual(-1,Step("CageMakers"),"fixture: the chapter has begun already");
            yield return Zone("ManyHands");
            var books=All<Transform>().FirstOrDefault(t=>t.name=="Ishen with his books");Assert.IsNotNull(books,"Ishen isn't on Many Hands' dock (OrielBuilder.StageApproach)");
            Assert.IsTrue(books.gameObject.activeInHierarchy,"Ishen isn't on the dock once Idra's cutters are home");
            var arrives=Point("oriel.charted");Assert.AreEqual("IshenArrives",arrives.dialogueNode);Assert.IsTrue(arrives.startsOnCrossing,"Ishen's arrival can be walked round");
            DialogueSystem.Current.AutoAdvance=true;
            var at=arrives.transform.position;Put(Lead,at+Vector3.back*3.5f);yield return Frames(2);Put(Lead,at+Vector3.forward*.5f);
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;yield return Settle();
            Assert.AreEqual(1,heard.Count(h=>h=="IshenArrives"),"walking up off the dock didn't bring Ishen's arrival: heard "+string.Join(", ",heard));
            Assert.IsTrue(Has("oriel.charted"),"Ishen's arrival didn't chart Oriel");Assert.AreEqual(0,Step("CageMakers"),"Ishen's arrival didn't start the chapter's story");
            Flag("oriel.charted",false);yield return Zone("ManyHandsHull");
            var warp=All<WarpBeacon>().Single(w=>w.scene=="Orrery");StringAssert.Contains("Ishen",warp.lockedPrompt,"the shut warp to the Orrery doesn't say who opens it");
            Put(Lead,warp.transform.position+Vector3.back*2);yield return Frames(3);warp.Interact();yield return Frames(3);
            Assert.IsFalse(SceneFlow.Current.Loading,"the warp to the Orrery opened before Ishen had told the party anything");
            Flag("oriel.charted");warp.Interact();yield return Arrive("Orrery");
            Assert.Less(Flat(Lead.transform.position,new Vector3(0,1,6)),4,"the warp didn't come out at the Orrery's arrival");
            Assert.AreEqual(1,Step("CageMakers"),"arriving in the Orrery didn't move the story on");
        }
    }
}
