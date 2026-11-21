using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Town life in the first settlements a player sees (November 12): the Decks, Tallow Drift and Lanternwick held to the standard the later towns set (guide 0.1:
    /// "the bodies of the people who live there, at least three looks per group, no two neighbours alike, and most of them with somewhere to be"), without putting anyone on a
    /// way a replay route or a story beat uses, in the fixed camera's line to anyone named or to a prompt, or inside a prop. Each rule is first shown to catch a resident put
    /// where it forbids.</summary>
    public sealed class TownLifeTests
    {
        static readonly string[] Towns={"Hub_Decks","TallowDrift","Lanternwick"};
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static Vector2 Plan(Vector3 p)=>new(p.x,p.z);
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Camera.main.aspect=16f/9;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at){var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}

        // ---------------------------------------------------------------- who lives here
        /// <summary>Everyone with no name who has a body: a townsperson standing, or one on an errand. Whichever beat they belong to.</summary>
        static GameObject[] Residents()=>All<PropTint>().Where(t=>t.transform.Find("Generated civilian")!=null&&t.GetComponent<Npc>()==null&&t.GetComponentInParent<CombatActor>(true)==null).Select(t=>t.gameObject).ToArray();
        /// <summary>Who they are dressed as: the body's own material.</summary>
        static string Look(GameObject who){var r=who.transform.Find("Generated civilian").GetComponentInChildren<SkinnedMeshRenderer>(true);return r!=null&&r.sharedMaterial!=null?r.sharedMaterial.name:"?";}
        static HashSet<GameObject> MovedByTheStory()=>new(All<Whereabouts>().SelectMany(w=>w.targets).Where(t=>t!=null));
        static readonly Dictionary<string,(int people,int errands)> Fewest=new(){{"Hub_Decks",(18,5)},{"TallowDrift",(8,2)},{"Lanternwick",(14,4)}};
        [UnityTest] public IEnumerator EachFirstTownIsLivedInByPeopleInSeveralLooksWithSomewhereToBe([ValueSource(nameof(Towns))] string zone)
        {
            yield return Zone(zone);
            var people=Residents();var errands=All<WalkLoop>();var story=MovedByTheStory();
            Assert.GreaterOrEqual(people.Length,Fewest[zone].people,$"{zone} has {people.Length} residents: it stands empty past its named few");
            Assert.GreaterOrEqual(errands.Length,Fewest[zone].errands,$"{zone} has {errands.Length} errands: its people stand about");
            var here=people.Where(p=>p.activeInHierarchy).ToArray();
            Assert.GreaterOrEqual(here.Select(Look).Distinct().Count(),3,$"{zone}'s people on a first visit wear fewer than three looks: "+string.Join(", ",here.Select(Look).Distinct()));
            for(int a=0;a<here.Length;a++)for(int b=a+1;b<here.Length;b++)
                if(Vector3.Distance(here[a].transform.position,here[b].transform.position)<3.2f)Assert.AreNotEqual(Look(here[a]),Look(here[b]),$"{zone}: {here[a].name} and {here[b].name} stand side by side in the same body");
            int busy=people.Count(p=>p.GetComponent<WalkLoop>()!=null||story.Contains(p));
            Assert.Greater(busy*2,people.Length,$"{zone}: only {busy} of {people.Length} residents walk an errand or move with the story: most of the town stands where it was put");
        }

        // ---------------------------------------------------------------- the ways
        /// <summary>Every leg a replay route walks in a zone, from the nearest spawn, as the dressing and the town's builder read them.</summary>
        static List<(Vector2 a,Vector2 b,string route)> Legs(string zone)
        {
            var legs=new List<(Vector2,Vector2,string)>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            var spawns=All<SpawnPoint>().Select(s=>Plan(s.transform.position)).ToArray();
            foreach(var file in System.IO.Directory.GetFiles(root,"*.json").OrderBy(f=>f))
            {
                QualityRoute route;try{route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));}catch{continue;}
                if(route?.steps==null)continue;string current=route.scene;Vector2? last=null;
                foreach(var step in route.steps)
                {
                    string scene=string.IsNullOrEmpty(step.expectedScene)?current:step.expectedScene;
                    if(scene!=current){current=scene;last=null;}
                    if(scene!=zone){last=null;continue;}
                    if(!step.navigate||step.approachTarget||step.approachPartner)continue;
                    var p=Plan(step.point);legs.Add((last??spawns.OrderBy(s=>Vector2.Distance(s,p)).First(),p,System.IO.Path.GetFileNameWithoutExtension(file)));last=p;
                }
            }
            return legs;
        }
        static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        /// <summary>Where an errand's loop comes within 1.6 m of a route (the rule Lanternwick's townsfolk and Rootmarket's are held to).</summary>
        static IEnumerable<string> OnTheWays(string who,IReadOnlyList<Vector2> loop,bool back,List<(Vector2 a,Vector2 b,string route)> legs)
        {
            int count=loop.Count-(back?1:0);
            for(int k=0;k<count;k++)
            {
                Vector2 a=loop[k],b=loop[(k+1)%loop.Count];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.2f));
                for(int s=0;s<=steps;s++){var p=Vector2.Lerp(a,b,s/(float)steps);foreach(var (ra,rb,route) in legs)if(Segment(p,ra,rb)<=1.6f)yield return $"{who} walks within 1.6 m of {route}'s leg {ra}->{rb} at {p}";}
            }
        }
        [UnityTest] public IEnumerator ErrandsKeepOffTheWaysAndComeBack([ValueSource(nameof(Towns))] string zone)
        {
            if(zone=="Hub_Decks")Flag("warpkey");
            yield return Zone(zone);
            var legs=Legs(zone);Assert.Greater(legs.Count,0,"fixture: replay routes walk "+zone);
            // The check can fail: an errand laid along the first leg a route walks is caught.
            Assert.IsNotEmpty(OnTheWays("control",new[]{legs[0].a,legs[0].b},true,legs).ToArray(),"control: an errand along a route's own leg passes");
            var errands=All<WalkLoop>();var faults=new List<string>();
            foreach(var walk in errands)faults.AddRange(OnTheWays(walk.name,walk.path.Select(Plan).ToArray(),walk.thereAndBack,legs));
            Assert.IsEmpty(faults,zone+":\n"+string.Join("\n",faults.Distinct().Take(20)));
            // Walked at speed, each goes the whole way and comes back, and never leaves its own line.
            var walking=errands.Where(w=>w.isActiveAndEnabled).ToArray();Assert.Greater(walking.Length,0,"fixture: someone is on an errand in "+zone);
            var seen=walking.ToDictionary(w=>w,w=>new bool[w.path.Length]);var back=walking.ToDictionary(w=>w,w=>false);float longest=0;
            foreach(var walk in walking)
            {
                float length=0;for(int k=1;k<walk.path.Length;k++)length+=Vector3.Distance(walk.path[k-1],walk.path[k]);if(!walk.thereAndBack)length+=Vector3.Distance(walk.path[^1],walk.path[0]);
                walk.speed*=6;walk.pause=.1f;longest=Mathf.Max(longest,(walk.thereAndBack?2:1)*length/walk.speed+walk.path.Length*.3f);
            }
            float deadline=Time.time+longest+3;
            while(Time.time<deadline&&back.Values.Any(b=>!b))
            {
                yield return null;
                foreach(var walk in walking)
                {
                    var at=Plan(walk.transform.position);var path=walk.path.Select(Plan).ToArray();var marks=seen[walk];
                    float off=float.PositiveInfinity;for(int k=0;k<path.Length;k++)off=Mathf.Min(off,Segment(at,path[k],path[(k+1)%path.Length]));
                    if(off>.3f)faults.Add($"{walk.name} strayed {off:0.00} m off its errand at {at}");
                    for(int k=0;k<path.Length;k++)if(Vector2.Distance(at,path[k])<.12f)marks[k]=true;
                    if(marks.All(m=>m)&&Vector2.Distance(at,path[0])<.12f)back[walk]=true;
                }
            }
            foreach(var walk in walking)if(!back[walk])faults.Add($"{walk.name} never finished its errand and came back (blocked: {walk.Blocked}, at {walk.transform.position})");
            Assert.IsEmpty(faults,zone+":\n"+string.Join("\n",faults.Distinct().Take(20)));
        }

        // ---------------------------------------------------------------- inside a prop, in the camera's line
        /// <summary>Things standing that a body can't be inside: drawn, not flat, not a person, not a sign.</summary>
        static Renderer[] Props()=>All<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<TMP_Text>()==null&&r.GetComponentInParent<PropTint>()==null&&r.GetComponentInParent<Npc>()==null
            &&r.GetComponentInParent<CombatActor>()==null&&!r.name.StartsWith("Floor detail")&&r.shadowCastingMode!=ShadowCastingMode.ShadowsOnly).ToArray();
        /// <summary>The rule NobodyStandsInsideAProp holds the named to (QaPlanWorldTests), and a solid box at the body's height.</summary>
        static string Inside(Vector3 at,Renderer[] props,Transform self)
        {
            foreach(var r in props)
            {
                var b=r.bounds;if(b.size.y<.25f||Mathf.Max(b.size.x,b.size.z)>9||b.max.y<at.y+.3f||b.min.y>at.y+1.8f)continue;
                if(Mathf.Abs(at.x-b.center.x)<b.extents.x*.75f+.3f&&Mathf.Abs(at.z-b.center.z)<b.extents.z*.75f+.3f)return "inside "+r.transform.root.name+"/"+r.name;
            }
            foreach(var c in Physics.OverlapBox(at+Vector3.up*1.05f,new Vector3(.35f,.7f,.35f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                if((self==null||!c.transform.IsChildOf(self))&&c.GetComponentInParent<PropTint>()==null&&c.GetComponentInParent<CombatActor>()==null&&!(c is CharacterController))return "in the solid "+c.transform.root.name+"/"+c.name;
            return null;
        }
        static bool InTheLine(Vector3 foot,float build,Vector3 target)
        {
            var box=new Bounds(foot+Vector3.up*.95f*build,new Vector3(.9f*build,1.9f*build,.9f*build));var eye=target+SightlineAudit.ToCamera*SightlineAudit.Distance;
            return box.IntersectRay(new Ray(eye,target-eye),out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore;
        }
        static string Hides(Vector3 foot,float build)
        {
            foreach(var npc in All<Npc>())foreach(float h in new[]{.4f,.9f,1.5f})if(InTheLine(foot,build,npc.transform.position+Vector3.up*h))return "between the camera and "+npc.speaker;
            foreach(var prompt in All<InteractionPrompt>().Where(p=>p is not Npc&&p.range>0))
            {
                var at=prompt.transform.position;float floor=at.y-(prompt is Bench||prompt is RepairBay?.8f:0);
                foreach(float h in new[]{.3f,.9f})if(InTheLine(foot,build,new Vector3(at.x,floor+h,at.z)))return "between the camera and \""+prompt.prompt+"\"";
            }
            return null;
        }
        [UnityTest] public IEnumerator NobodyLivesInsideAPropOrInTheCamerasLineToAnyoneNamedOrAPrompt([ValueSource(nameof(Towns))] string zone)
        {
            yield return Zone(zone);
            // Every beat at once: whoever the story brings in is measured where they will stand.
            var people=Residents();foreach(var p in people)p.SetActive(true);
            foreach(var walk in All<WalkLoop>())walk.enabled=false;
            Physics.SyncTransforms();var props=Props();
            // The checks can fail: a body put in the middle of the biggest piece of furniture here, and one a step in front of a named person, are both caught.
            var furniture=props.Where(r=>r.bounds.size.y>.7f&&r.bounds.size.y<2.5f&&Mathf.Max(r.bounds.size.x,r.bounds.size.z)<4).OrderByDescending(r=>r.bounds.size.x*r.bounds.size.z).First();
            Assert.IsNotNull(Inside(new Vector3(furniture.bounds.center.x,furniture.bounds.min.y,furniture.bounds.center.z),props,null),"control: someone standing in "+furniture.name+" passes");
            var named=All<Npc>().First(n=>n.gameObject.activeInHierarchy);
            var toward=SightlineAudit.ToCamera;toward.y=0;toward.Normalize();
            Assert.IsNotNull(Hides(named.transform.position+toward*1.2f,1),"control: someone a step in front of "+named.speaker+" from the camera passes");
            var faults=new List<string>();
            foreach(var person in people)
            {
                float build=person.transform.localScale.y;var walk=person.GetComponent<WalkLoop>();
                var spots=walk!=null?walk.path.SelectMany((p,k)=>Enumerable.Range(0,9).Select(s=>Vector3.Lerp(p,walk.path[walk.thereAndBack?Mathf.Min(k+1,walk.path.Length-1):(k+1)%walk.path.Length],s/8f))).ToArray():new[]{person.transform.position};
                foreach(var at in spots)
                {
                    if(walk==null&&Inside(at,props,person.transform) is string inside)faults.Add($"{person.name} at {Plan(at)} stands {inside}");
                    if(Hides(at,build) is string hides)faults.Add($"{person.name} at {Plan(at)} stands {hides}");
                }
            }
            Assert.IsEmpty(faults,zone+":\n"+string.Join("\n",faults.Distinct().Take(20)));
        }

        // ---------------------------------------------------------------- where the story has them
        static GameObject Who(string name)=>Residents().FirstOrDefault(p=>p.name==name)??All<Transform>().FirstOrDefault(t=>t.name==name)?.gameObject;
        static void Here(string name,bool here,string when)
        {
            var who=Who(name);Assert.IsNotNull(who,"fixture: nobody called "+name);
            Assert.AreEqual(here,who.activeInHierarchy,$"{when}: {name} is {(who.activeInHierarchy?"there":"not there")}");
        }
        [UnityTest] public IEnumerator TheDecksMorningGivesWayToTheWorkingDayAndCabinOneMovesOut()
        {
            GameServices.Current.NewGame();Flag(OpeningMarketSequence.Requested);
            yield return Zone("Hub_Decks","Office");
            const string Queue="At the front of the breakfast queue",Shopper="A shopper at the west stall",Child="A child between the tables",Apprentice="Hal's apprentice",Packer="Packing up cabin 01",Leaving="Bound for Nacre";
            GameObject Hatch()=>All<Transform>().Single(t=>t.name=="Closed private pressure hatch"&&Vector2.Distance(Plan(t.position),new Vector2(-33,12.25f))<.8f).gameObject;
            Here(Queue,true,"the first morning");Here(Child,true,"the first morning");Here(Shopper,false,"the first morning");Here(Apprentice,false,"the first morning");
            Assert.IsTrue(Hatch().activeInHierarchy,"the first morning: cabin 01's hatch stands open with nobody in the doorway");Here(Packer,false,"the first morning");Here(Leaving,false,"the first morning");
            Flag("warpkey");yield return Zone("Hub_Decks");
            Here(Queue,false,"back with the survey key");Here(Child,false,"back with the survey key");Here(Shopper,true,"back with the survey key");Here(Apprentice,true,"back with the survey key");
            Flag("bossdown.Cantor");yield return Zone("Hub_Decks");
            Assert.IsFalse(Hatch().activeInHierarchy,"after the Cantor: cabin 01's hatch is shut on a household packing");
            Here(Packer,true,"after the Cantor");Here("Cabin 01's packed crate",true,"after the Cantor");Here(Leaving,false,"after the Cantor");
            Flag("chapter2.homecoming");yield return Zone("Hub_Decks");
            Assert.IsTrue(Hatch().activeInHierarchy,"after the homecoming: cabin 01's hatch still stands open with nobody packing");
            Here(Packer,false,"after the homecoming");Here("Cabin 01's packed crate",false,"after the homecoming");Here(Leaving,true,"after the homecoming");
        }
        [UnityTest] public IEnumerator TallowsRestingResidentIsAtTheConsoleOnceTheLineLeadsNowhere()
        {
            yield return Zone("TallowDrift");
            Here("Resting",true,"before the line is found");Here("Worried at the console",false,"before the line is found");Here("Asking what it means",false,"before the line is found");
            Flag("tallow.lineObserved");yield return Zone("TallowDrift");
            Here("Resting",false,"once the line leads nowhere");Here("Worried at the console",true,"once the line leads nowhere");Here("Asking what it means",true,"once the line leads nowhere");
        }

        [UnityTest] public IEnumerator LanternwicksSquareWaitsForWordAndItsArchiveFillsAsTheRoadIsWon()
        {
            GameObject At(float x,float z)=>Residents().Single(p=>p.GetComponent<WalkLoop>()==null&&Vector2.Distance(Plan(p.transform.position),new Vector2(x,z))<.1f);
            GameObject Reader()=>All<WalkLoop>().Single(w=>w.name=="Reading at the archive racks").gameObject;
            yield return Zone("Lanternwick");
            Assert.IsTrue(At(-9.4f,-7.2f).activeInHierarchy&&At(7.5f,-8.4f).activeInHierarchy,"before the relay: nobody waits on the courier square for word");
            Assert.IsFalse(At(-9f,93.6f).activeInHierarchy||At(-7.6f,94.8f).activeInHierarchy,"before the relay: the archive wall's watchers are there already");
            Assert.IsTrue(At(-7.6f,88.4f).activeInHierarchy,"before the record: the reader isn't at the archive's door");Assert.IsFalse(Reader().activeInHierarchy,"before the record: the reader is among the racks");
            Flag("bossdown.HessaKorl");Flag("ishen.recordRestored");yield return Zone("Lanternwick");
            Assert.IsFalse(At(-9.4f,-7.2f).activeInHierarchy||At(7.5f,-8.4f).activeInHierarchy,"with Hessa down: the pair still wait on the square");
            Assert.IsTrue(At(-9f,93.6f).activeInHierarchy&&At(-7.6f,94.8f).activeInHierarchy,"with Hessa down: nobody watches the archive's wall light");
            Assert.IsFalse(At(-7.6f,88.4f).activeInHierarchy,"with the record back: the reader still stands at the door");Assert.IsTrue(Reader().activeInHierarchy,"with the record back: nobody reads at the racks");
        }

        // ---------------------------------------------------------------- Mira's walk home
        /// <summary>D195 left her walk unmeasured: GeneratedAnimator played it at .9 m/s over 2.1 held to no slower than .7, so the foot she stood on slid forward under her a
        /// hand's breadth a second. On her own stride the planted sole holds. Measured on her real walk home after the first job (.9 m/s, her ScriptedWalk), the first stretch on
        /// her profile and the rest, as the control, with it taken away.</summary>
        [UnityTest] public IEnumerator MiraPlantsHerFeetOnHerWalkHome()
        {
            GameServices.Current.NewGame();Flag(OpeningMarketSequence.Requested);
            yield return Zone("Hub_Decks","Office");
            var mira=All<Npc>().Single(n=>n.speaker=="Mira");var walk=mira.GetComponent<ScriptedWalk>();var driver=mira.GetComponentInChildren<GeneratedAnimator>();
            Assert.IsNotNull(driver.strideProfile,"Mira walks on no measured stride: her walk plays at the floor rate whatever her pace, and her planted sole slides");
            Assert.AreEqual("Mira",driver.strideProfile.name,"Mira walks on someone else's stride");
            Put(PartyController.Current.Active,new Vector3(4,0,-5));
            // Her visible soles: each toe bone carrying the toe marker read off her generated boot (as ContinuousReview samples a hero's).
            var rig=driver.GetComponentInChildren<Animator>();var profile=driver.strideProfile;
            Transform leftToe=rig.GetBoneTransform(HumanBodyBones.LeftToes),rightToe=rig.GetBoneTransform(HumanBodyBones.RightToes);Assert.IsNotNull(leftToe,"fixture: Mira's toes");
            Flag(OpeningMarketSequence.Repaired);float deadline=Time.time+4;
            while(walk.Phase!=ScriptedWalk.Stage.Exiting&&Time.time<deadline)yield return null;
            Assert.AreEqual(ScriptedWalk.Stage.Exiting,walk.Phase,"fixture: Mira sets off home once the job is done");
            // Each sole through its stance (.2 to .4 of the walk's cycle, the second foot half a cycle on: the window her stride is measured in), as the Walk state's own phase
            // says. "The lower of the two toes" was tried first and read .55 m/s either way: through most of a cycle the lower toe is rolling or swinging, not planted.
            // Distance over time, summed, not a mean of each frame's speed: a frame's sample has the body moved this frame and the bones posed the last, and the test runner's
            // frame times swing, so per-frame ratios weighed the pose's motion by the wrong frame's length (on the deck it read -.137 and .055 against the editor's .034 and .171).
            float ownDistance=0,ownTime=0,noneDistance=0,noneTime=0;int ownFrames=0,noneFrames=0;Vector3[] last=null;float changeAt=Time.time+6.5f;int walkState=Animator.StringToHash("Walk");
            while(walk.Phase==ScriptedWalk.Stage.Exiting)
            {
                yield return null;
                if(Time.time>=changeAt&&driver.strideProfile!=null){driver.strideProfile=null;last=null;continue;}
                var info=rig.GetCurrentAnimatorStateInfo(0);var now=new[]{leftToe.TransformPoint(profile.leftToe),rightToe.TransformPoint(profile.rightToe)};
                bool walking=info.shortNameHash==walkState&&!rig.IsInTransition(0)&&Time.deltaTime>0;var travel=mira.transform.forward;travel.y=0;travel.Normalize();
                if(last!=null&&walking)for(int side=0;side<2;side++)
                {
                    float phase=Mathf.Repeat(info.normalizedTime-side*.5f,1);
                    if(phase<=.2f||phase>=.4f)continue;float moved=Vector3.Dot(now[side]-last[side],travel);
                    if(driver.strideProfile!=null){ownDistance+=moved;ownTime+=Time.deltaTime;ownFrames++;}else{noneDistance+=moved;noneTime+=Time.deltaTime;noneFrames++;}
                }
                last=walking?now:null;
            }
            driver.strideProfile=profile;
            Assert.Greater(ownFrames,40,"fixture: stance frames of her walk on her own stride");Assert.Greater(noneFrames,40,"fixture: stance frames of her walk without it");
            float onOwn=ownDistance/ownTime,onNone=noneDistance/noneTime;TestContext.WriteLine($"Mira's planted toe slides {onOwn:0.000} m/s forward on her stride and {onNone:0.000} m/s without it");
            // The editor's reading of the same walk (CivilianStride.Slip): about .04 on her stride, .18 at the floor rate.
            Assert.Greater(onNone,.12f,"control: without her stride the planted sole doesn't slide, so this can't tell");
            Assert.Less(Mathf.Abs(onOwn),.07f,$"on her own stride Mira's planted sole still slides {onOwn:0.000} m/s");
        }
    }
}
