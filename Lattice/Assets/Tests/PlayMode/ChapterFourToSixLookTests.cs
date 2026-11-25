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
    /// <summary>What the whole-game capture of November 22 found in chapters 4 to 6 that can be measured (D359): a piece marked to fade whose material can't draw a fade (the
    /// gantry over Many Hands' spine stood solid across the walk, s28/043 and s30/114), the main walks of Many Hands with something tall and solid between the camera and the
    /// lead (the gantry; the freight yard's stack at the hall's arrival, s29/070), and the Orrery's view drawn so far back over Ishen's talk that the ship was a speck (s38/080).</summary>
    public sealed class ChapterFourToSixLookTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static CombatActor Lead=>PartyController.Current.Active;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static Vector3 Floor(Vector2 at,float y=1)
        {
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(at.x,y,at.y),out var hit,2.5f,NavMesh.AllAreas),$"fixture: no floor near ({at.x}, {at.y})");
            return hit.position;
        }
        static string Chapter(string zone)=>zone switch{"ManyHands"=>"chapter5","Lastlight" or "FrozenHost"=>"chapter6",_=>"chapter7"};

        // ================================================================== a fade the player can draw
        /// <summary>Every material on a piece the scene fades (<see cref="SceneryOcclusion"/>) can draw the fade: the shader's fade is a variant, on only where the material carries
        /// _OCCFADE_ON. The fade's own amount is set on the renderer whether or not the material can draw it, so a check of the amount (the freight hall's, D346) passes while
        /// the player draws the piece solid. Many Hands' materials are painted afresh by another zone's builder (the Neighbourhood Afloat paints them for its own district), and
        /// a fresh paint dropped the keyword from the city's plate, wood, cloth and panel: the gantry over the spine stood solid across the walk (s28/043, s30/114).</summary>
        [UnityTest] public IEnumerator EveryPieceTheSceneFadesCanDrawItsFade([Values("ManyHands","Lastlight","FrozenHost","GlassChoir","OrielSkyfields")] string zone)
        {
            DevLoadout.Apply(Chapter(zone));yield return Zone(zone);
            var fading=All<SceneryOcclusion>();
            List<string> Faults()
            {
                var faults=new List<string>();
                foreach(var piece in fading)foreach(var drawn in piece.GetComponentsInChildren<Renderer>(true))
                {
                    if(drawn is not MeshRenderer&&drawn is not SkinnedMeshRenderer)continue;
                    foreach(var material in drawn.sharedMaterials)
                        if(material!=null&&material.HasProperty("_OccFade")&&!material.IsKeywordEnabled("_OCCFADE_ON"))faults.Add($"{piece.name} fades but its material {material.name} can't draw a fade");
                }
                return faults.Distinct().ToList();
            }
            if(fading.Length==0)Assert.Pass(zone+" fades nothing");
            // The broken state, built here: one fading piece given a copy of its material with the fade's variant off is what the check has to find.
            var first=fading.SelectMany(f=>f.GetComponentsInChildren<Renderer>(true)).First(r=>r.sharedMaterials.Any(m=>m!=null&&m.HasProperty("_OccFade")));
            var kept=first.sharedMaterials;var broken=kept.Select(m=>{if(m==null||!m.HasProperty("_OccFade"))return m;var copy=new Material(m){name=m.name+" (no fade)"};copy.DisableKeyword("_OCCFADE_ON");return copy;}).ToArray();
            first.sharedMaterials=broken;
            Assert.IsTrue(Faults().Any(f=>f.Contains("(no fade)")),"the check doesn't see a fading piece whose material can't draw the fade");
            first.sharedMaterials=kept;foreach(var copy in broken)if(copy!=null&&copy.name.EndsWith("(no fade)"))Object.Destroy(copy);
            var found=Faults();
            Assert.IsEmpty(found,zone+":\n"+string.Join("\n",found));
        }

        // ================================================================== Many Hands' main walks
        /// <summary>Every way feet go in the city, the spine and the walk from the dock to the freight hall's door first: every two metres of them, nothing
        /// tall stands drawn and solid between the fixed camera and the lead's chest. Something tall on the line either fades (and its material can draw the fade) or isn't
        /// there: the gantry's beam stood solid over the spine (s28/043, s30/114), and a container stack two high stood across the freight hall's arrival (s29/070).</summary>
        [UnityTest,Timeout(900000)] public IEnumerator ManyHandsMainWalksKeepTheLeadInSightFromTheCamera()
        {
            DevLoadout.Apply("chapter5");Flag("hands.iloAboard");Flag("hands.claimRead");Flag("bossdown.Hulljack");
            yield return Zone("ManyHands");
            // D414: scenery opens its hole only for a lead nothing draws through (HeroThrough draws the rest as a clean line); whether it can is what this reads.
            Lead.GetComponent<HeroThrough>().enabled=false;
            static float Faded(Renderer r){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);return block.GetFloat("_OccFade");}
            // Whether the line meets the piece's own triangles: a box round a room (the galley) or a frame (the drying racks) crosses lines that pass through its open middle.
            // A mesh that can't be read counts as its box.
            static bool Meets(Renderer r,Ray ray,float far)
            {
                var filter=r.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;
                if(mesh==null||!mesh.isReadable)return true;
                var world=r.transform.localToWorldMatrix;var v=mesh.vertices;var tris=mesh.triangles;
                for(int i=0;i+2<tris.Length;i+=3)
                {
                    Vector3 a=world.MultiplyPoint3x4(v[tris[i]]),b=world.MultiplyPoint3x4(v[tris[i+1]]),c=world.MultiplyPoint3x4(v[tris[i+2]]);
                    Vector3 e1=b-a,e2=c-a,p=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-7f)continue;
                    float inv=1/det;var s=ray.origin-a;float u=Vector3.Dot(s,p)*inv;if(u<0||u>1)continue;
                    var q=Vector3.Cross(s,e1);float w=Vector3.Dot(ray.direction,q)*inv;if(w<0||u+w>1)continue;
                    float t=Vector3.Dot(e2,q)*inv;if(t>0&&t<far)return true;
                }
                return false;
            }
            static bool CanFade(Renderer r)=>r.sharedMaterials.All(m=>m==null||!m.HasProperty("_OccFade")||m.IsKeywordEnabled("_OCCFADE_ON"));
            // Whether the line really crosses a fading piece and not only the box round it, by the piece's own measured cells (as SceneryOcclusion asks): the dock door's box
            // spans the spine, and the walk goes through its open middle.
            static bool Crosses(SceneryOcclusion fade,Vector3 eye,Vector3 chest)
            {
                if(fade.opening.size.sqrMagnitude>0)
                {
                    Vector3 a=fade.transform.InverseTransformPoint(eye),b=fade.transform.InverseTransformPoint(chest);float depth=b.z-a.z;bool through=Mathf.Abs(depth)>.001f;
                    for(int face=0;face<2&&through;face++){var at=Vector3.Lerp(a,b,((face==0?fade.opening.min.z:fade.opening.max.z)-a.z)/depth);if(Mathf.Abs(at.x-fade.opening.center.x)>fade.opening.extents.x||at.y<fade.opening.min.y||at.y>fade.opening.max.y)through=false;}
                    if(through)return false;
                }
                if(fade.shape==null||fade.shape.Length==0)return true;
                Vector3 from=fade.transform.InverseTransformPoint(eye)-fade.shapeLow,along=fade.transform.InverseTransformPoint(chest)-fade.shapeLow-from;float enter=0,leave=1;
                for(int axis=0;axis<3;axis++)
                {
                    float size=fade.shapeCell*(axis==0?fade.shapeX:axis==1?fade.shapeY:fade.shapeZ);
                    if(Mathf.Abs(along[axis])<1e-5f){if(from[axis]<0||from[axis]>size)return false;continue;}
                    float lo=-from[axis]/along[axis],hi=(size-from[axis])/along[axis];enter=Mathf.Max(enter,Mathf.Min(lo,hi));leave=Mathf.Min(leave,Mathf.Max(lo,hi));if(enter>leave)return false;
                }
                float stride=fade.shapeCell*.5f/Mathf.Max(along.magnitude,1e-4f);
                for(float t=enter;t<=leave;t+=stride)
                {
                    var p=from+along*t;int bit=(Mathf.Clamp((int)(p.z/fade.shapeCell),0,fade.shapeZ-1)*fade.shapeY+Mathf.Clamp((int)(p.y/fade.shapeCell),0,fade.shapeY-1))*fade.shapeX+Mathf.Clamp((int)(p.x/fade.shapeCell),0,fade.shapeX-1);
                    if((fade.shape[bit>>3]&(1<<(bit&7)))!=0)return true;
                }
                return false;
            }
            // What is drawn on the camera's line to the lead's chest and as tall as the fade's rule (ZoneKit.TallEnoughToBlock, 2.5 m): scenery, never a body. A renderer's box
            // is what this measures, so the deck, its walls and its floor detail (which span the whole ship) are left out by their size.
            List<Renderer> InTheWay()
            {
                var eye=Camera.main.transform.position;var chest=Lead.transform.position+Vector3.up*1.1f;var ray=new Ray(eye,chest-eye);float far=Vector3.Distance(eye,chest)-.4f;
                return Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.enabled&&r.gameObject.activeInHierarchy
                    &&r.bounds.size.y>=2.5f&&r.bounds.size.x<40&&r.bounds.size.z<40&&r.GetComponentInParent<CombatActor>()==null&&r.GetComponentInParent<Npc>()==null
                    &&r.GetComponentInParent<WalkLoop>()==null&&r.sharedMaterials.Any(m=>m!=null&&m.renderQueue<2500)&&r.bounds.IntersectRay(ray,out float at)&&at<far).ToList();
            }
            // The city's ways (ManyHandsBuilder.Hands.Ways), named: the spine and the freight yard first, then everywhere else feet go.
            var walks=new[]
            {
                ("the freight spine",new[]{new Vector2(-4,-60),new Vector2(0,-55),new Vector2(0,-40),new Vector2(0,-20),new Vector2(0,3),new Vector2(0,20),new Vector2(0,23)}),
                ("the walk to the freight hall",new[]{new Vector2(0,-57),new Vector2(12,-59.4f),new Vector2(32,-59),new Vector2(32,-53.5f)}),
                ("the dock",new[]{new Vector2(-4,-60),new Vector2(-12,-59.6f),new Vector2(-22,-58.6f)}),
                ("the cradle",new[]{new Vector2(0,-45),new Vector2(-8.6f,-45)}),
                ("the raft bay",new[]{new Vector2(0,-24),new Vector2(-14.5f,-24),new Vector2(-24,-24),new Vector2(-31,-23),new Vector2(-32.5f,-22.5f),new Vector2(-49,-21),new Vector2(-56,-18.6f)}),
                ("the ferry walk",new[]{new Vector2(-24,-24),new Vector2(-29,-29),new Vector2(-42.5f,-32),new Vector2(-44.75f,-32.5f),new Vector2(-44.5f,-33.6f),new Vector2(-48,-34.3f)}),
                ("the north quay",new[]{new Vector2(-24,3),new Vector2(-40,2),new Vector2(-58,-4),new Vector2(-58,-11.5f),new Vector2(-58,-15.5f)}),
                ("the arcade walk",new[]{new Vector2(-24,-2),new Vector2(-24,-11.5f),new Vector2(-24,-19)}),
                ("the arcade",new[]{new Vector2(-34,3),new Vector2(0,3),new Vector2(50,3),new Vector2(56,2.4f),new Vector2(66,3.6f)}),
                ("the galley",new[]{new Vector2(56,2.4f),new Vector2(55.6f,4.6f),new Vector2(55.6f,10.4f),new Vector2(64.8f,10.4f)}),
                ("the garden",new[]{new Vector2(0,20),new Vector2(16,20.4f),new Vector2(27,24.2f),new Vector2(44,24.2f),new Vector2(60,24.2f),new Vector2(73.4f,24.2f)}),
                ("Marrow-1",new[]{new Vector2(44,24.2f),new Vector2(44,52)}),("Marrow-2",new[]{new Vector2(60,24.2f),new Vector2(60,52)}),
                ("the cabins",new[]{new Vector2(0,19.6f),new Vector2(-16,20.4f),new Vector2(-30,20),new Vector2(-52,20),new Vector2(-52,30.6f),new Vector2(-52,40.6f),new Vector2(-52,50.2f),new Vector2(-58,50.2f)}),
                ("the gallery",new[]{new Vector2(0,40.4f),new Vector2(7,45.4f),new Vector2(12.6f,48.6f)}),("the gallery's west",new[]{new Vector2(0,40.4f),new Vector2(-11,42),new Vector2(-17.4f,42)}),
            };
            var faults=new List<string>();bool shownFailing=false;Vector3 last=Lead.transform.position;
            foreach(var (name,way) in walks)
                for(int i=1;i<way.Length;i++)
                {
                    float leg=Vector2.Distance(way[i-1],way[i]);
                    for(float d=0;d<leg;d+=2f)
                    {
                        var p=Vector2.Lerp(way[i-1],way[i],d/leg);
                        // The lamp gallery stands 3.6 m up: the floor nearest a point taken from over both levels.
                        Assert.IsTrue(NavMesh.SamplePosition(new Vector3(p.x,4.2f,p.y),out var floor,4.6f,NavMesh.AllAreas),$"fixture: no floor near ({p.x}, {p.y})");
                        // A step along a way, or a jump to the next way's start: the camera follows smoothly, so after a jump it has to arrive before its line means anything.
                        bool jumped=Vector3.Distance(last,floor.position)>3;last=floor.position;Put(Lead,floor.position);
                        yield return new WaitForSecondsRealtime(jumped?1.6f:.35f);
                        foreach(var r in InTheWay())
                        {
                            var fade=r.GetComponentInParent<SceneryOcclusion>();
                            var eye=Camera.main.transform.position;var chest=Lead.transform.position+Vector3.up*.95f;
                            if(fade!=null&&!Crosses(fade,eye,Vector3.MoveTowards(chest,eye,.15f)))continue;
                            if(fade==null&&!Meets(r,new Ray(eye,(chest-eye).normalized),Vector3.Distance(eye,chest)-.4f))continue;
                            if(fade==null)faults.Add($"{r.name} ({r.bounds.size.y:0.0} m tall) stands between the camera and the lead on {name} at ({p.x:0.0}, {p.y:0.0}) and doesn't fade");
                            else if(Faded(r)<=0)faults.Add($"{r.name} is marked to fade but stood solid between the camera and the lead on {name} at ({p.x:0.0}, {p.y:0.0})");
                            else if(!CanFade(r))faults.Add($"{r.name} fades between the camera and the lead on {name} at ({p.x:0.0}, {p.y:0.0}), but its material can't draw it: the player shows it solid");
                            else if(r.name.StartsWith("Container stack"))faults.Add($"{r.name} stands across {name} at ({p.x:0.0}, {p.y:0.0}): the party is drawn through a hole in it for as long as they walk past");
                            // The broken state, built here once: the same piece with its fade off is what the check has to find.
                            if(!shownFailing&&fade!=null)
                            {
                                fade.enabled=false;yield return new WaitForSecondsRealtime(.6f);
                                Assert.IsTrue(InTheWay().Any(o=>o==r&&Faded(o)<=0),"the check doesn't see a piece whose fade is off standing solid in the way");
                                fade.enabled=true;shownFailing=true;yield return new WaitForSecondsRealtime(.6f);
                            }
                        }
                    }
                }
            Assert.IsTrue(shownFailing,"fixture: nothing tall ever came between the camera and the lead on Many Hands' walks (the gantry stands over the spine)");
            Assert.IsEmpty(faults.Distinct(),string.Join("\n",faults.Distinct()));
        }

        // ================================================================== a still of each zone whose fading pieces were given back their fade
        /// <summary>For the reviewers (D359): in each zone whose materials were given the fade's variant, the lead stood behind one of the pieces that couldn't draw it, from the game's
        /// own camera, a still at 1920 by 1080 into Builds/quality/look/&lt;CH456_FADE_STILLS&gt;/. Renders only when that names a run. The piece is found by its material.</summary>
        [UnityTest,Timeout(1500000)] public IEnumerator RendersAFadeInEachRepairedZone()
        {
            string run=System.Environment.GetEnvironmentVariable("CH456_FADE_STILLS");
            if(string.IsNullOrEmpty(run))Assert.Ignore("renders only when CH456_FADE_STILLS names a run");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/look",run));System.IO.Directory.CreateDirectory(folder);
            var zones=new (string zone,string[] materials)[]
            {
                ("BraidedRiver",new[]{"xylos-bark","xylos-vine"}),("WalkingCanopy",new[]{"xylos-vine"}),
                ("Driftreed",new[]{"wend-reed-thatch","wend-reed-bundle","wend-plank","wend-cloth","wend-mat-dye","wend-hearth-dark","wend-lantern"}),
                ("MeridianCrawl",new[]{"crawl-timber","crawl-window","crawl-brass","crawl-stone","crawl-rope","crawl-dark"}),
                ("Continuance",new[]{"continuance-hot","continuance-arc","continuance-coolant","continuance-lamp-warm","continuance-void","continuance-paint-"}),
                ("StillwaterChamber",new[]{"chamber-lamp-teal","chamber-lamp-white"}),
            };
            static float Faded(Renderer r){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);return block.GetFloat("_OccFade");}
            var done=new List<string>();var missed=new List<string>();
            foreach(var (zone,materials) in zones)
            {
                yield return Cleanup();yield return Boot();DevLoadout.Apply("starter");
                SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+30;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
                foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
                foreach(var fight in All<EncounterVolume>())foreach(var box in fight.GetComponents<Collider>())box.enabled=false;
                foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
                yield return new WaitForSecondsRealtime(1f);
                var pieces=All<SceneryOcclusion>().Where(f=>f.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m!=null&&materials.Any(n=>m.name.StartsWith(n))))).ToList();
                string shot=null;
                foreach(var piece in pieces.Take(12))
                {
                    var bodies=piece.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer).ToArray();if(bodies.Length==0)continue;
                    var box=bodies[0].bounds;foreach(var b in bodies)box.Encapsulate(b.bounds);
                    var camera=Camera.main;var ahead=camera.transform.forward;float pitch=Mathf.Asin(Mathf.Clamp(-ahead.y,.2f,1));ahead.y=0;ahead.Normalize();
                    float lift=Mathf.Max(.5f,box.center.y-box.min.y-1);
                    foreach(float extra in new[]{1.5f,3f,.5f,5f})
                    {
                        var want=box.center+ahead*(lift/Mathf.Tan(pitch)+extra);want.y=box.min.y+.5f;
                        if(!NavMesh.SamplePosition(want,out var floor,2.5f,NavMesh.AllAreas))continue;
                        Put(Lead,floor.position);yield return new WaitForSecondsRealtime(2.2f);
                        if(!bodies.Any(r=>Faded(r)>0))continue;
                        camera=Camera.main;var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);var was=camera.targetTexture;
                        try
                        {
                            camera.targetTexture=rt;camera.Render();var before=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();RenderTexture.active=before;
                            shot=zone+"-"+piece.name.Replace(' ','-').Replace("'","")+".png";System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,shot),image.EncodeToPNG());
                        }
                        finally{camera.targetTexture=was;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
                        TestContext.WriteLine($"FADE_STILL {zone} {piece.name} lead=({floor.position.x:0.0}, {floor.position.y:0.0}, {floor.position.z:0.0}) {shot}");
                        break;
                    }
                    if(shot!=null)break;
                }
                if(shot!=null)done.Add(shot);else missed.Add(zone+" ("+pieces.Count+" pieces tried)");
            }
            TestContext.WriteLine("FADE_STILLS "+folder+" "+string.Join(",",done));
            Assert.IsEmpty(missed,"no fading piece was caught between the camera and the lead in: "+string.Join(", ",missed));
        }

        // ================================================================== the Orrery's view over Ishen's talk
        /// <summary>While Ishen names the moons the view draws back to take in the first rail's gates, but no further than leaves the lead ship 60 pixels long on a 1080-line
        /// screen: drawn back over the first two rails as well, it stood 337 m from the ship and the ship was a speck of about 28 (s38/080). Measured the length of the ship's
        /// drawn hull along its heading, on the game's own camera, with the ship where the talk stops it.</summary>
        [UnityTest] public IEnumerator WhileIshenNamesTheMoonsTheShipStaysSixtyPixelsLong()
        {
            DevLoadout.Apply("chapter7");Flag("oriel.charted");GameServices.Current.State.questSteps["CageMakers"]=0;
            yield return Zone("Orrery");
            var view=All<VantageView>().Single();var camera=Camera.main;
            // The game's own screen (1920 by 1080): a test runner's window is often narrower, and a narrower frame stands further back to take in the same width.
            camera.aspect=16f/9;
            Put(Lead,new Vector3(0,1,view.transform.position.z-3));yield return new WaitForSecondsRealtime(3f);
            Assert.IsTrue(view.Holding,"at Ishen's talk the view doesn't draw back over the machine");
            float Length()
            {
                var bodies=Lead.GetComponentsInChildren<Renderer>().Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                Assert.IsNotEmpty(bodies,"fixture: the lead ship draws nothing");
                var box=bodies[0].bounds;foreach(var b in bodies)box.Encapsulate(b.bounds);
                var ahead=Lead.transform.forward;ahead.y=0;ahead.Normalize();
                float half=Mathf.Abs(box.extents.x*ahead.x)+Mathf.Abs(box.extents.z*ahead.z);
                var a=camera.WorldToScreenPoint(box.center-ahead*half);var b2=camera.WorldToScreenPoint(box.center+ahead*half);
                return Vector2.Distance(a,b2)*1080f/camera.pixelHeight;
            }
            // The broken state, built here: the same ship seen from twice as far back is shorter by half, and the measure has to say so.
            float here=Length();var back=camera.transform.position;var look=camera.transform.forward;
            camera.transform.position=Lead.transform.position-look*Vector3.Distance(back,Lead.transform.position)*2;float far=Length();camera.transform.position=back;
            Assert.Less(far,here*.6f,"fixture: the measure doesn't shrink with the camera's distance");
            Debug.Log($"CH456_ORRERY_SHIP length={here:0.0}px camera={Vector3.Distance(back,Lead.transform.position):0.0}m");
            Assert.GreaterOrEqual(here,60,$"while Ishen names the moons the lead ship is {here:0} px long (camera {Vector3.Distance(back,Lead.transform.position):0} m away): a speck");
            var steward=All<OrreryRail>().Single(r=>r.railName=="The Steward");
            var gate=steward.On(steward.gateAt[1],steward.GateRadius);var seen=camera.WorldToViewportPoint(new Vector3(gate.x,1,gate.y));
            Assert.IsTrue(seen.z>0&&seen.x>0&&seen.x<1&&seen.y>0&&seen.y<1,"the Steward's gates aren't in the frame while Ishen names the moons");
            camera.ResetAspect();
        }
    }
}
