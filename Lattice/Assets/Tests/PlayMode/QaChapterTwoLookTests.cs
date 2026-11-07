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
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November eye pass on the Lantern Road. What a person saw in the captures: a floor that lost its stone, an arch lying on its back, curtains like a bar chart,
    /// people behind barrels. Every check here fails on the build it was written against, and says what the player would see go wrong.</summary>
    public sealed class QaChapterTwoLookTests
    {
        static readonly string[] Everywhere={"Hub_Decks","Hub_CinderHalo","Sorrel_Ridges","Hushwell","Gullet_Tunnel","TallowApproach","TallowDrift","WickReach","Lanternwick","LanternGorge","SunkenRelay"};
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            // The runner's own window isn't 16:9: what's read through the viewport has to be the view a player has.
            Camera.main.aspect=16f/9;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        static CombatActor Lead=>PartyController.Current.Active;
        static WarpBeacon WarpTo(string scene)=>All<WarpBeacon>().Single(w=>w.scene==scene);
        /// <summary>Nothing starts while the hero is put about the zone: a fight's framing would move the camera these checks read through.</summary>
        static void ClearFights(){foreach(var fight in All<EncounterVolume>())Flag("clear."+fight.encounterId);}
        static Bounds BoundsOf(Component root)
        {
            var parts=root.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer or SkinnedMeshRenderer).ToArray();
            if(parts.Length==0)return new Bounds(root.transform.position,Vector3.zero);
            var box=parts[0].bounds;foreach(var part in parts)box.Encapsulate(part.bounds);return box;
        }
        static bool Hides(Bounds box,Vector3 target)
        {
            var eye=target+SightlineAudit.ToCamera*SightlineAudit.Distance;var ray=new Ray(eye,target-eye);
            return box.IntersectRay(ray,out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore;
        }
        static bool Inside(Vector3 view)=>view.z>0&&view.x>.02f&&view.x<.98f&&view.y>.02f&&view.y<.98f;
        static float Faded(SceneryOcclusion scenery)
        {
            var block=new MaterialPropertyBlock();float fade=0;
            foreach(var r in scenery.GetComponentsInChildren<Renderer>()){r.GetPropertyBlock(block);fade=Mathf.Max(fade,block.GetFloat("_OccFade"));}
            return fade;
        }
        /// <summary>How much fine detail the camera shows on the ground, as a share of how bright it is: .012 to .03 on the cracked stone in the captures, .001 to .004 on the flat colour. Read from the
        /// camera's own image, away from the HUD, as the median of four patches so a prop or a hero in one of them doesn't decide it.</summary>
        static float FloorDetail()
        {
            const int W=960,H=540;
            var pixels=Grab(W,H);var patches=new[]{new RectInt(300,160,150,70),new RectInt(550,160,150,60),new RectInt(75,230,150,100),new RectInt(700,230,150,100)};
            var scores=new List<float>();
            foreach(var patch in patches)
            {
                float rough=0,bright=0;int count=0;
                for(int y=patch.yMin+2;y<patch.yMax-2;y++)for(int x=patch.xMin+2;x<patch.xMax-2;x++)
                {
                    float near=0;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)near+=pixels[(y+dy)*W+x+dx].grayscale;
                    float own=pixels[y*W+x].grayscale;rough+=Mathf.Abs(own-near/25);bright+=own;count++;
                }
                scores.Add(rough/Mathf.Max(.01f,bright));
            }
            scores.Sort();return (scores[1]+scores[2])/2;
        }
        /// <summary>What the game's camera draws, without the HUD. Row 0 is the bottom of the screen.</summary>
        static Color[] Grab(int W,int H)
        {
            var camera=Camera.main;
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                return image.GetPixels();
            }
            finally
            {
                camera.targetTexture=target;RenderTexture.active=active;rt.Release();
                Object.Destroy(rt);Object.Destroy(image);
            }
        }
        /// <summary>Whether the real mesh, not its box, stands between the lens and a point: a temporary collider on a layer nothing else uses, as SightlineAudit does.</summary>
        static bool MeshHides(Component root,Vector3 target)
        {
            const int Probe=31;var probes=new List<GameObject>();
            try
            {
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.sharedMesh==null||filter.GetComponent<TextMeshPro>()!=null)continue;
                    var probe=new GameObject("Sightline probe"){layer=Probe};probe.transform.SetParent(filter.transform,false);probe.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;probes.Add(probe);
                }
                Physics.SyncTransforms();
                var eye=Camera.main.transform.position;var to=target-eye;
                return Physics.Raycast(eye,to,to.magnitude-SightlineAudit.NearIgnore,1<<Probe,QueryTriggerInteraction.Collide);
            }
            finally{foreach(var probe in probes)Object.DestroyImmediate(probe);Physics.SyncTransforms();}
        }
        /// <summary>The corners of a sign's words, in the world.</summary>
        static IEnumerable<Vector3> Corners(TextMeshPro sign)
        {
            sign.ForceMeshUpdate();var box=sign.textBounds;var middle=sign.transform.TransformPoint(box.center);
            var across=sign.transform.right*box.extents.x;var up=sign.transform.up*box.extents.y;
            yield return middle-across-up;yield return middle-across+up;yield return middle+across-up;yield return middle+across+up;
        }
        /// <summary>Where the HUD stands in a ground zone, in the viewport: the status panel, the objective, the button strip and the skill bar.</summary>
        static readonly Rect[] HudPanels={Rect.MinMaxRect(0,.72f,.29f,1),Rect.MinMaxRect(.69f,.76f,1,1),Rect.MinMaxRect(.09f,0,.56f,.075f),Rect.MinMaxRect(.65f,0,1,.15f)};

        // ---------------------------------------------------------------- the floor (item 0)
        [UnityTest] public IEnumerator NothingDrawsAMeshWithNoUvsOverATexture()
        {
            foreach(var zone in Everywhere)
            {
                yield return Zone(zone);
                foreach(var filter in All<MeshFilter>())
                {
                    var view=filter.GetComponent<MeshRenderer>();if(view==null||!view.enabled||!view.gameObject.activeInHierarchy||filter.sharedMesh==null)continue;
                    // In play a static body's filter holds its combined batch, with uvs from whatever it was merged with. A collision body's own mesh is still on its collider.
                    var body=filter.GetComponent<MeshCollider>();var mesh=body!=null&&body.sharedMesh!=null?body.sharedMesh:filter.sharedMesh;
                    if(mesh.HasVertexAttribute(VertexAttribute.TexCoord0))continue;
                    foreach(var material in view.sharedMaterials)
                        if(material!=null&&material.HasProperty("_BaseMap")&&material.GetTexture("_BaseMap")!=null)
                            Assert.Fail($"{zone}: {filter.name} draws {mesh.name}, which has no uvs, with {material.name}: it paints one flat colour from a single texel, right over the textured floor at the same depth");
                }
            }
        }
        [UnityTest] public IEnumerator TheFloorKeepsItsStoneAfterAZoneChange()
        {
            foreach(var (from,zone,spot) in new[]{("LanternGorge","Lanternwick",new Vector3(0,.3f,46)),("Lanternwick","LanternGorge",new Vector3(-2,.6f,10)),("LanternGorge","SunkenRelay",new Vector3(0,.2f,10))})
            {
                yield return Zone(from);
                yield return Zone(zone);
                var landform=All<MeshCollider>().Single(c=>c.GetComponent<Renderer>()!=null);
                var filter=landform.GetComponent<MeshFilter>();
                Assert.IsTrue(filter==null||filter.sharedMesh==null,$"{zone}: the collision body still draws its whole mesh: the floor's stone comes and goes with the order of a zone load");
                Assert.IsTrue(CameraRig.Scenery(landform),$"{zone}: the ground camera no longer treats the landform as scenery, so it stops clearing the walls");
                Put(Lead,spot);foreach(var member in PartyController.Current.members)if(member!=Lead)Put(member,spot+new Vector3(1.5f,0,-1));
                yield return new WaitForSecondsRealtime(.8f);
                float detail=FloorDetail();
                Assert.Greater(detail,.006f,$"{zone}, come to by way of {from}, shows a flat colour where its cracked stone floor should be (detail {detail:0.0000})");
            }
        }
        [UnityTest] public IEnumerator ABumpInTheFloorDoesNotDropAToonBand()
        {
            foreach(var (zone,least,most) in new[]{("Lanternwick",new Vector2(-14,-20),new Vector2(14,40)),("LanternGorge",new Vector2(-6,0),new Vector2(6,30)),("SunkenRelay",new Vector2(-7,-6),new Vector2(7,18))})
            {
                yield return Zone(zone);
                var sun=All<Light>().Single(l=>l.type==LightType.Directional&&l.gameObject.scene.name==zone);var toSun=-sun.transform.forward;
                var floor=All<MeshFilter>().Single(f=>f.name.EndsWith(" floor"));var mesh=floor.sharedMesh;
                Assert.IsTrue(mesh.isReadable,zone+": the floor is merged into one static batch with the collision mesh that paints over it");
                float steps=floor.GetComponent<Renderer>().sharedMaterial.GetFloat("_RampSteps");
                int Band(Vector3 normal)=>Mathf.FloorToInt(Vector3.Dot(normal,toSun)*steps+.5f);
                var world=mesh.vertices;var normals=mesh.normals;int level=Band(Vector3.up),off=0,read=0;Vector3 first=default;
                for(int i=0;i<world.Length;i++)
                {
                    var p=world[i];if(p.x<least.x||p.x>most.x||p.z<least.y||p.z>most.y)continue;
                    read++;if(Band(normals[i])!=level){if(off==0)first=p;off++;}
                }
                Assert.Greater(read,50,zone+": fixture: floor in the window");
                Assert.AreEqual(0,off,$"{zone}: {off} points of level floor fall in another band of the toon ramp (first at {first}): a hard, stair-stepped dark shape on the ground with nothing casting it");
            }
        }

        // ---------------------------------------------------------------- curtains (items 1, 2)
        [UnityTest] public IEnumerator CurtainsAreLightThatFadesOffTheFloor()
        {
            foreach(var (zone,tallest) in new[]{("SunkenRelay",4.3f),("WickReach",6f)})
            {
                yield return Zone(zone);
                var strands=All<LineRenderer>().Where(l=>l.name=="Curtain strand").ToArray();
                Assert.Greater(strands.Length,15,zone+": fixture: the curtain has its strands");
                foreach(var strand in strands)
                {
                    var material=strand.sharedMaterial;
                    Assert.IsNotNull(material.GetTexture("_BaseMap"),zone+": a strand has no texture: a flat opaque bar, not a curtain of light");
                    Assert.AreEqual((float)BlendMode.One,material.GetFloat("_DstBlend"),zone+": a strand covers what's behind it instead of glowing over it");
                    Assert.AreEqual("curtain_soft",material.GetTexture("_BaseMap").name,zone+": drawn on the flare, a thin streak, a strand is a spark halfway up its own length");
                    // The Reach's outer strands thin away on purpose, so its ends don't stop in a row of teeth: all but they keep the old floor, and they have to really fall away.
                    float across=Mathf.Abs(strand.transform.TransformPoint(strand.GetPosition(0)).x);
                    if(zone=="WickReach"&&across>36)Assert.Less(strand.startColor.a,across>46?.3f:.62f,zone+": the curtain's ends don't thin away: they stop in a row of teeth");
                    else Assert.Greater(strand.startColor.a,.6f,zone+": a strand isn't brightest at the floor");
                    Assert.Less(strand.endColor.a,.03f,zone+": a strand doesn't fade to nothing at its top: it ends in a hard edge up across the HUD");
                    Assert.Less(strand.transform.TransformPoint(strand.GetPosition(1)).y,tallest,zone+": a strand climbs over the HUD");
                }
                int lengths=strands.Select(s=>Mathf.RoundToInt(Vector3.Distance(s.GetPosition(0),s.GetPosition(1))*10)).Distinct().Count();
                Assert.GreaterOrEqual(lengths,3,zone+": every strand is the same, a row of bars");
            }
        }
        [UnityTest] public IEnumerator TheSealGlowsAtTheFloorAndFadesUpward()
        {
            yield return Zone("SunkenRelay");
            var strands=All<LineRenderer>().Where(l=>l.name=="Curtain strand").ToArray();
            Assert.Greater(strands.Length,15,"fixture: the seal has its strands");
            Put(Lead,new Vector3(0,.1f,112));foreach(var member in PartyController.Current.members)if(member!=Lead)Put(member,new Vector3(4,.1f,110));
            yield return new WaitForSecondsRealtime(.8f);
            const int W=960,H=540;
            var bare=Grab(W,H);
            // The door owns its strands: shut it the way the fight does.
            var seal=All<Membrane>().Single(m=>m.name=="Great Lantern seal");seal.SetOpen(false);
            Assert.IsTrue(strands[0].gameObject.activeInHierarchy&&strands[0].enabled,"fixture: a shut seal shows its strands");
            var lit=Grab(W,H);
            // How much the seal adds to the picture, along its own plane between two heights above the floor. The camera's yaw slants the seal across the frame, so it's read point by point.
            float Added(float low,float high)
            {
                float sum=0;int count=0;
                for(float along=-6;along<=6;along+=.1f)for(float rise=low;rise<=high;rise+=.1f)
                {
                    var view=Camera.main.WorldToViewportPoint(new Vector3(along,rise,116.5f));int x=Mathf.FloorToInt(view.x*W),y=Mathf.FloorToInt(view.y*H);
                    if(x<0||x>=W||y<0||y>=H)continue;
                    sum+=lit[y*W+x].grayscale-bare[y*W+x].grayscale;count++;
                }
                Assert.Greater(count,100,"fixture: the seal is in view");
                return sum/count;
            }
            float foot=Added(.1f,1.1f),crown=Added(2.2f,3.2f);
            Assert.Greater(foot,.02f,$"the closed seal hardly shows on the floor ({foot:0.000}): a row of sparks, not a curtain of light");
            Assert.Less(crown,foot*.6f,$"the seal is as bright at its top ({crown:0.000}) as at the floor ({foot:0.000}): bars rising off the floor, up under the HUD");
        }
        [UnityTest] public IEnumerator TheCurtainComesDownInView()
        {
            yield return Zone("WickReach");
            var strands=All<LineRenderer>().Where(l=>l.name=="Curtain strand").ToArray();
            Put(Lead,new Vector3(21,1,228));
            Flag("wick.relay1");yield return new WaitForSecondsRealtime(.6f);
            Flag("wick.relay2");
            float deadline=Time.unscaledTime+3;while(!Has("wick.gateOpen")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("wick.gateOpen"),"fixture: two relays open the gate");
            yield return new WaitForSecondsRealtime(1.2f);
            var camera=Camera.main;int seen=0;
            foreach(var strand in strands)
                if(strand.gameObject.activeInHierarchy&&strand.startColor.a>.05f&&Inside(camera.WorldToViewportPoint(strand.transform.TransformPoint(strand.GetPosition(0)))))seen++;
            Assert.GreaterOrEqual(seen,3,"'Curtain's coming down' plays with no curtain on screen: the player lit the relay and has nothing to see");
            Assert.IsTrue(Inside(camera.WorldToViewportPoint(Lead.transform.position)),"showing the curtain lost the ship");
        }
        [UnityTest] public IEnumerator TheGatesSignSitsOnTheGateAndGoesWithIt()
        {
            yield return Zone("WickReach");
            var sign=All<TextMeshPro>().Single(t=>t.text=="LANTERN GATE");var strand=All<LineRenderer>().First(l=>l.name=="Curtain strand");
            Assert.Less(sign.transform.position.y,1,"the sign floats over nothing, up above the flight plane at the top edge of the screen");
            Assert.Less(Mathf.Abs(sign.transform.position.z-strand.transform.TransformPoint(strand.GetPosition(0)).z),3,"the sign doesn't sit on the gate");
            Flag("wick.relay1");Flag("wick.relay2");
            yield return new WaitForSecondsRealtime(5);
            Assert.IsFalse(sign.gameObject.activeInHierarchy,"LANTERN GATE still names a stretch of empty space after the curtain has come down");
        }

        // ---------------------------------------------------------------- Tallow (items 3, 4, 6, 7)
        [UnityTest] public IEnumerator TallowsHullHoldsAsAPaintedHull()
        {
            yield return Zone("TallowApproach");
            var hull=All<Transform>().First(t=>t.name=="TallowStationHull");var material=hull.GetComponentInChildren<Renderer>().sharedMaterial;
            Assert.Less(material.GetColor("_BaseColor").maxColorComponent,.8f,"the hull's pale texture under a 1.4 sun clips to lavender white");
            Assert.Less(material.GetFloat("_ReceiveShadows"),.01f,"scene shadows land on the hull as blocky stair-stepped blobs");
            var pipeline=GraphicsSettings.currentRenderPipeline;var split=pipeline.GetType().GetProperty("cascade2Split");Assert.IsNotNull(split,"fixture: the pipeline has a cascade split");
            Assert.GreaterOrEqual((float)split.GetValue(pipeline),.45f,"what stands 20 m from the camera falls in the far shadow cascade, at 7 cm a texel: a stair on every floor");
        }
        [UnityTest] public IEnumerator TheAnchorMarkerIsNotAWarp()
        {
            yield return Zone("TallowApproach");
            var marker=All<TallowMarkerDrift>().Single().marker;var warp=All<WarpBeacon>().First();
            var markerLook=marker.GetComponentInChildren<Renderer>().sharedMaterial;var warpLook=warp.GetComponentInChildren<Renderer>().sharedMaterial;
            Assert.AreNotEqual(warpLook,markerLook,"the detached anchor wears the warp's horned beacon at the warp's size: this shape means warp");
        }
        [UnityTest] public IEnumerator TheHeroStandsClearAtTheLaunch()
        {
            yield return Zone("TallowDrift");
            var launch=All<DockingPad>().Single(d=>d.scene=="TallowApproach");
            var hatch=BoundsOf(GameObject.Find("Dock pressure lift hatch").transform);
            var spot=new Vector3(launch.transform.position.x,.05f,launch.transform.position.z);
            foreach(var (height,part) in new[]{(.1f,"feet"),(1f,"chest")})
                Assert.IsFalse(Hides(hatch,spot+Vector3.up*height),$"on the launch spot the outer hatch frame hides the hero's {part}");
            Put(Lead,new Vector3(0,.05f,-6));PromptService.FindInteraction(Lead.transform.position);
            Assert.AreEqual(launch,PromptService.Interaction,"the launch doesn't answer from the door of its lobby");
            Put(Lead,new Vector3(0,.05f,-11));PromptService.FindInteraction(Lead.transform.position);
            Assert.AreEqual(launch,PromptService.Interaction,"the launch doesn't answer from the far end of its lobby");
            var words=All<TextMeshPro>().Single(t=>t.text.Contains("PRESSURE LIFT"));words.ForceMeshUpdate();
            Assert.Less(words.textBounds.size.x,4.4f,$"\"{words.text}\" is wider than the lobby's 5 m at the sign style's size: both ends run into its low walls");
        }
        [UnityTest] public IEnumerator TallowsFloorPaintIsInView()
        {
            yield return Zone("TallowDrift");
            var parts=All<MeshRenderer>();
            var galley=parts.First(r=>r.name=="Floor detail PictoGalley").bounds;var frame=BoundsOf(GameObject.Find("Life support hatch -1").transform);
            foreach(var (x,z) in new[]{(galley.min.x,galley.min.z),(galley.max.x,galley.min.z),(galley.min.x,galley.max.z),(galley.max.x,galley.max.z)})
                Assert.IsFalse(Hides(frame,new Vector3(x,.02f,z)),"the galley's picture lies behind the hatch frame: all that shows is a pale tuft sticking out of it");
            var cabins=parts.First(r=>r.name=="Floor detail PictoCabins").bounds;var sign=All<TextMeshPro>().Single(t=>t.text=="SERVICE / REST CABINS").GetComponent<Renderer>().bounds;
            bool crosses=cabins.max.x>sign.min.x&&cabins.min.x<sign.max.x&&cabins.max.z>sign.min.z&&cabins.min.z<sign.max.z;
            Assert.IsFalse(crosses,"SERVICE / REST CABINS is printed right on top of the bed pictogram");
        }

        // ---------------------------------------------------------------- the lanternwick reader's gorge and town
        [UnityTest] public IEnumerator RelightingAGorgeRelayChangesWhatYouSee()
        {
            yield return Zone("LanternGorge");ClearFights();
            var points=All<DiscoveryPoint>().Where(p=>p.flag.StartsWith("gorge.relay")).ToArray();Assert.AreEqual(3,points.Length,"fixture: three relays");
            foreach(var relay in All<RelayLantern>().Where(r=>r.flag.StartsWith("gorge.relay")))
            {
                var point=points.Single(p=>p.flag==relay.flag);var at=point.transform.position;
                float floor=at.y;Assert.Less(relay.glow.transform.position.y-floor,4.5f,relay.flag+": its light is up at the crown: no glow falls on the floor where you relight it");
                var low=relay.crystals.Where(c=>c!=null&&c.bounds.max.y-floor<4&&Vector3.Distance(c.bounds.center,at)<5).ToArray();
                Assert.Greater(low.Length,0,relay.flag+": the tower's crown is off the top of the screen from its prompt, and nothing within sight lights when you press A");
                Put(Lead,new Vector3(at.x,at.y+.2f,at.z));yield return new WaitForSecondsRealtime(.6f);
                Assert.IsTrue(Inside(Camera.main.WorldToViewportPoint(new Vector3(low[0].bounds.center.x,low[0].bounds.max.y,low[0].bounds.center.z))),relay.flag+": the lantern that lights isn't in view from its prompt");
            }
        }
        [UnityTest] public IEnumerator ThingsToTakeReadAsWhatTheyAre()
        {
            yield return Zone("LanternGorge");
            var lamp=All<DiscoveryPoint>().Single(p=>p.flag=="pell.lampFound").visual;
            lamp.SetActive(true);var lying=BoundsOf(lamp.transform);Assert.Less(lying.size.y,Mathf.Max(lying.size.x,lying.size.z),"Pell's chewed lamp is a clean upright pedestal: it reads as ordinary scenery");
            var crate=All<DiscoveryPoint>().Single(p=>p.flag=="unsworn.rationsFound").visual;
            Assert.IsNotNull(crate.GetComponentInChildren<Light>(true),"the ration crate is a near copy of every other black crate, and nothing says which one the prompt means");
            var kit=All<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<TextMeshPro>()==null&&!r.name.EndsWith(" floor")&&!r.name.EndsWith(" cliffs")).ToArray();
            foreach(var light in All<Light>().Where(l=>l.name=="Lamp light"))
            {
                var at=new Vector2(light.transform.position.x,light.transform.position.z);
                Assert.IsTrue(kit.Any(r=>Vector2.Distance(at,new Vector2(r.bounds.center.x,r.bounds.center.z))<Mathf.Max(1.5f,r.bounds.extents.magnitude)),$"a pool of light at {at} has no lamp");
            }
            var lantern=All<RelayLantern>().First(r=>r.flag=="gorge.relay1");Flag("gorge.relay1");
            yield return null;yield return null;
            var ring=Telegraph.Live.FirstOrDefault(m=>m!=null&&m.owner==lantern);Assert.IsNotNull(ring,"fixture: a lit relay shows its ward");
            Assert.Less(ring.Radius-ring.Inner,.2f,"the ward's ring is as bold as an attack marking and cuts right through them");
        }
        [UnityTest] public IEnumerator BrekkHasHisShedAndTheWestWallStaysUnderTheLens()
        {
            yield return Zone("Lanternwick");
            var brekk=All<Npc>().Single(n=>n.speaker=="Brekk");
            var shed=All<Transform>().Where(t=>t.name=="MusterLeanTo").Select(t=>t.position).ToArray();
            Assert.IsTrue(shed.Any(p=>Vector3.Distance(p,brekk.transform.position)<6&&Mathf.Abs(p.y-brekk.transform.position.y)<3),"the quest says the orchard shed, and he stands in the open with nothing behind him");
            var underLens=new Vector3(-39.5f,30,62);
            Assert.IsTrue(Physics.Raycast(underLens,Vector3.down,out var hit,40,~0,QueryTriggerInteraction.Ignore),"fixture: ground under the camera");
            Assert.Less(hit.point.y,9,"the west wall comes up to the lens on the way to the orchard gate: a third of the screen is one smear of strata");
        }
        [UnityTest] public IEnumerator LowThingsUnderTheLensKeepTheirShape()
        {
            yield return Zone("Lanternwick");
            var home=All<SceneryOcclusion>().Where(s=>s.name=="CanyonHouse").OrderBy(s=>Vector3.Distance(s.transform.position,new Vector3(-12,2.5f,40))).First();
            var at=home.transform.position;
            Put(Lead,new Vector3(at.x+5.1f,.2f,at.z+14.05f));
            yield return new WaitForSecondsRealtime(.9f);
            Assert.Less(Faded(home),.05f,"a low home passing under the camera turns into a dotted ghost across the floor, with its shadow still there");
        }

        // ---------------------------------------------------------------- relays (item 5)
        static (float dark,float share) DeadShare(Texture own,Texture dead)
        {
            const int Size=128;var rt=RenderTexture.GetTemporary(Size,Size,0,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var image=new Texture2D(Size,Size,TextureFormat.RGB24,false);
            try
            {
                Graphics.Blit(own,rt);RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,Size,Size),0,0);var a=image.GetPixels();
                Graphics.Blit(dead,rt);RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,Size,Size),0,0);var b=image.GetPixels();
                float sum=0;int changed=0;
                for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>.25f){sum+=b[i].grayscale;changed++;}
                return (changed>0?sum/changed:1,changed/(float)a.Length);
            }
            finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.Destroy(image);}
        }
        [UnityTest] public IEnumerator DeadGlassIsDarkAndLitGlassGlows()
        {
            var block=new MaterialPropertyBlock();
            foreach(var (zone,flag) in new[]{("WickReach","wick.relay1"),("LanternGorge","gorge.relay1"),("Lanternwick","ishen.recordRestored")})
            {
                yield return Zone(zone);yield return null;yield return null;
                var relay=All<RelayLantern>().Single(r=>r.flag==flag);var body=relay.GetComponentInChildren<MeshRenderer>();
                body.GetPropertyBlock(block);
                Assert.IsTrue(block.HasTexture("_BaseMap"),$"{zone}: a dead relay wears its painted amber glass, which in full sun looks softly lit, so the one you can relight doesn't stand out");
                var dead=block.GetTexture("_BaseMap");var own=body.sharedMaterial.GetTexture("_BaseMap");
                Assert.AreNotEqual(own,dead,$"{zone}: the dead glass is the lit texture");
                var (dark,share)=DeadShare(own,dead);
                Assert.Greater(share,.03f,$"{zone}: nothing on the tower changes when it goes dark ({share:0.000})");
                Assert.Less(dark,.6f,$"{zone}: the dead glass is still bright ({dark:0.00}, where the lit amber is about .74)");
                Flag(flag);yield return null;yield return null;
                body.GetPropertyBlock(block);
                Assert.AreEqual(own,block.GetTexture("_BaseMap"),$"{zone}: a relit relay keeps its dead glass");
                Assert.Greater(((Vector4)block.GetColor("_EmissionColor")).magnitude,1,$"{zone}: a relit relay doesn't glow");
            }
            yield return Zone("WickReach");
            int fallen=0;
            foreach(var tower in All<Transform>().Where(t=>t.name.StartsWith("Fallen relay ")))
            {
                fallen++;var body=tower.GetComponentInChildren<MeshRenderer>();body.GetPropertyBlock(block);
                Assert.IsTrue(block.HasTexture("_BaseMap"),tower.name+": a fallen tower's glass stays bright yellow while the objective says 0/2");
            }
            Assert.Greater(fallen,0,"fixture: fallen relays in the reach");
        }

        // ---------------------------------------------------------------- the Wick Reach (items 8, 17)
        [UnityTest] public IEnumerator TheReachsDebrisVariesAndKeepsOffWhatMatters()
        {
            yield return Zone("WickReach");
            var wreckage=All<Transform>().Where(t=>t.name.StartsWith("Wreckage ")).ToArray();
            Assert.GreaterOrEqual(wreckage.Select(t=>t.name).Distinct().Count(),3,"one broken cage repeated, with only a new rotation, is nearly all the dressing in a long zone");
            var keep=All<WarpBeacon>().Select(w=>w.transform.position).Concat(All<DockingPad>().Select(d=>d.transform.position)).ToArray();
            Assert.Greater(keep.Length,1,"fixture: a warp and a dock");
            float slide=1/Mathf.Tan(48*Mathf.Deg2Rad);
            foreach(var piece in wreckage.Concat(All<Transform>().Where(t=>t.name.StartsWith("Fallen relay "))))
            {
                var box=BoundsOf(piece);
                var shows=new Vector2(box.center.x,box.center.z-(1-box.center.y)*slide);
                foreach(var at in keep)
                    Assert.Greater(Vector2.Distance(shows,new Vector2(at.x,at.z))-box.extents.magnitude,3,$"{piece.name} shows on screen right on the gate at {at}: its claws look sunk in floating debris");
            }
        }
        [UnityTest] public IEnumerator TheWickHasASkyOfItsOwn()
        {
            yield return null;
            Assert.AreNotEqual(GameCatalog.Find<ZoneDef>("TallowApproach").skybox,GameCatalog.Find<ZoneDef>("WickReach").skybox,"the warp arrives under the same stars in the same place");
        }

        // ---------------------------------------------------------------- people and what stands in front of them (items 10, 15, 17)
        [UnityTest] public IEnumerator NobodyStandsBehindTheScenery()
        {
            foreach(var zone in new[]{"Lanternwick","SunkenRelay"})
            {
                yield return Zone(zone);
                var scenery=All<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponentInParent<Npc>(true)==null&&r.GetComponent<TextMeshPro>()==null
                    &&!r.name.EndsWith(" floor")&&!r.name.EndsWith(" cliffs")&&r.name!="Sign plate").ToArray();
                foreach(var person in All<Npc>())
                    foreach(var (height,part) in new[]{(.4f,"legs"),(.9f,"body"),(1.5f,"chest")})
                        foreach(var piece in scenery)
                            Assert.IsFalse(Hides(piece.bounds,person.transform.position+Vector3.up*height),$"{zone}: {person.speaker}'s {part} are behind {piece.transform.root.name} from the fixed camera, through their whole conversation");
            }
        }
        [UnityTest] public IEnumerator ArchesStandOnTheirFeet()
        {
            foreach(var zone in new[]{"Lanternwick","LanternGorge"})
            {
                yield return Zone(zone);
                int read=0;
                foreach(var view in All<MeshRenderer>().Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.name.StartsWith("AncelaneArch")))
                {
                    read++;var box=view.bounds;
                    Assert.Greater(box.size.y,box.size.z*1.8f,$"{zone}: an arch lies on its back ({box.size}): a huge pale horseshoe across the floor, a shadow with nothing casting it, and two dark blades where it ends");
                }
                Assert.Greater(read,0,zone+": fixture: the zone has its arch");
                // The way out of the town and the way into the relay are gates: an arch to go through, not a stub of broken column.
                var gate=WarpTo(zone=="Lanternwick"?"LanternGorge":"SunkenRelay").GetComponentInChildren<MeshRenderer>();
                StringAssert.StartsWith("AncelaneArch",gate.sharedMaterial.name,zone+": the gate the prompt names is "+gate.sharedMaterial.name+": there's no gate there");
            }
        }
        [UnityTest] public IEnumerator TheArchFramesTheArrivalAndLeavesTheShopAlone()
        {
            yield return Zone("Lanternwick");
            var arch=All<SceneryOcclusion>().Single(s=>s.name=="Market arch");
            yield return new WaitForSecondsRealtime(.8f);
            var camera=Camera.main;var box=BoundsOf(arch);
            Assert.IsTrue(Inside(camera.WorldToViewportPoint(new Vector3(box.center.x,1.5f,box.center.z))),"the first look at the town is a bare floor: the arch isn't in it");
            Assert.Less(Faded(arch),.05f,"a hole bites into the arch around a hero standing in plain view in its opening");
            foreach(var member in PartyController.Current.members)
                foreach(var (height,part) in new[]{(.4f,"legs"),(.95f,"body"),(1.5f,"chest")})
                    Assert.IsFalse(MeshHides(arch,member.transform.position+Vector3.up*height),$"{member.character}'s {part} are behind a leg of the arch on the first look at the town");
            Put(Lead,new Vector3(-4,.2f,17));yield return new WaitForSecondsRealtime(.8f);
            Assert.Less(Faded(arch),.05f,"the arch fades out at Corra's stall: the hero makes a building vanish at the shop");
            var corra=All<Npc>().Single(n=>n.speaker=="Corra");
            foreach(var post in All<Transform>().Where(t=>t.name=="LampPost"))
                Assert.IsFalse(Hides(BoundsOf(post),corra.transform.position+Vector3.up*.4f),"a lamp post stands in front of Corra's legs");
        }
        [UnityTest] public IEnumerator TheTownHasALaidWayThroughIt()
        {
            yield return Zone("Lanternwick");
            var mesh=All<MeshFilter>().Single(f=>f.name.EndsWith(" floor")).sharedMesh;var tones=mesh.colors;
            Assert.AreEqual(mesh.vertexCount,tones.Length,"the town's floor is one colour from wall to wall");
            Color At(float x,float z){var world=mesh.vertices;int best=0;float near=float.MaxValue;for(int i=0;i<world.Length;i++){float d=(world[i].x-x)*(world[i].x-x)+(world[i].z-z)*(world[i].z-z);if(d<near){near=d;best=i;}}return tones[best];}
            Assert.Greater(At(0,30).grayscale-At(22,10).grayscale,.08f,"the main street doesn't read as laid stone against the ground beside it");
        }

        // ---------------------------------------------------------------- the relay's exits (item 13)
        [UnityTest] public IEnumerator TheRelaysExitsAreWhatTheySay()
        {
            Flag("relay.hessaConfronted");
            yield return Zone("SunkenRelay");
            var lift=WarpTo("Lanternwick");var climb=WarpTo("LanternGorge");
            StringAssert.Contains("HushwellLift",lift.GetComponentInChildren<Renderer>().sharedMaterial.name,"'Ride the relay lift' is a stub of broken column with nothing to ride");
            StringAssert.Contains("HushwellScaffold",climb.GetComponentInChildren<Renderer>().sharedMaterial.name,"'Climb out' is a stub of broken column with nothing to climb");
            Assert.IsTrue(All<FlagGate>().Any(g=>g.flag==lift.requiredFlag&&g.targets.Any(t=>t!=null&&t.GetComponent<Light>()!=null)),"the lift's lamp doesn't come on when it will run");
            Flag("relay.hessaYielded");
            Put(Lead,new Vector3(-5,.1f,158));foreach(var member in PartyController.Current.members)if(member!=Lead)Put(member,new Vector3(-8,.1f,155));
            yield return new WaitForSecondsRealtime(1);
            PromptService.FindInteraction(Lead.transform.position);
            Assert.AreEqual(lift,PromptService.Interaction,"the route's stop in front of the lift gets no prompt");
            var sign=All<TextMeshPro>().Single(t=>t.text==lift.prompt&&t.GetComponentInParent<Npc>()==null);var box=sign.GetComponent<Renderer>().bounds;var camera=Camera.main;
            foreach(var corner in new[]{box.min,box.max,new Vector3(box.min.x,box.center.y,box.max.z),new Vector3(box.max.x,box.center.y,box.min.z)})
            {
                var view=camera.WorldToViewportPoint(corner);
                Assert.IsFalse(view.x<.275f&&view.y>.72f,"the lift's sign runs under the status panel where the prompt shows: 'AY LIFT'");
                Assert.IsTrue(view.x>0&&view.x<1&&view.y>0&&view.y<1,"the lift's sign is cut off by the screen's edge where the prompt shows");
            }
        }

        // ---------------------------------------------------------------- the archive (item 14)
        [UnityTest] public IEnumerator TheArchiveIsAPlaceYouCanSee()
        {
            yield return Zone("Lanternwick");
            Put(Lead,new Vector3(3,.2f,90));
            yield return new WaitForSecondsRealtime(.8f);
            var door=All<SceneryOcclusion>().FirstOrDefault(s=>s.name=="Archive door");
            Assert.IsNotNull(door,"no door stands behind Ishen: the archive is two dark blades and some column bases");
            var doorway=BoundsOf(door);
            Assert.IsTrue(Physics.Raycast(new Vector3(3.2f,1.5f,93),Vector3.forward,out var hit,12,~0,QueryTriggerInteraction.Ignore),"nothing stands behind Ishen");
            Assert.Less(hit.point.z,99,$"no archive wall or door is in view where you talk to Ishen: the nearest thing behind him is {hit.collider.name} at z {hit.point.z:0}");
            Assert.IsTrue(Inside(Camera.main.WorldToViewportPoint(new Vector3(doorway.center.x+2.6f,1,doorway.min.z))),"the archive's door is off the top of the screen where you talk to Ishen");
            var ishen=All<Npc>().Single(n=>n.speaker=="Ishen");
            Assert.Less(Vector3.Distance(ishen.transform.position,new Vector3(doorway.center.x,0,doorway.min.z)),9,"Ishen stands nowhere near his archive");
        }

        // ---------------------------------------------------------------- underground (item 16)
        [UnityTest] public IEnumerator TheSunkenRelayReadsAsUnderground()
        {
            yield return Zone("SunkenRelay");
            var key=All<Light>().Single(l=>l.type==LightType.Directional&&l.gameObject.scene.name=="SunkenRelay");
            Assert.Less(key.intensity,.4f,"a station under a canyon lit by a sun");
            Assert.Less(key.shadowStrength,.3f,"hard sun-like shadows fall across a buried floor");
            Assert.Less(RenderSettings.ambientSkyColor.grayscale,.25f,"the wall tops and floor are evenly lit, as if open to the sky");
            var cliffView=All<MeshFilter>().Single(f=>f.name.EndsWith(" cliffs"));var cliffs=cliffView.sharedMesh;var up=cliffs.normals;var tones=cliffs.colors;
            Assert.AreEqual(cliffs.vertexCount,tones.Length,"the wall tops are as bright as the floor");
            Assert.IsTrue(cliffView.GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("_VCOLOR_ON"),"the walls carry their dark in vertex colour, and their material doesn't draw it: the tops stay lit as if open to the sky");
            float tops=0;int read=0;for(int i=0;i<up.Length;i++)if(up[i].y>.9f){tops+=tones[i].grayscale;read++;}
            Assert.Greater(read,0,"fixture: walls have tops");Assert.Less(tops/read,.6f,"the wall tops are lit as if open to the sky");
            var floor=All<MeshFilter>().Single(f=>f.name.EndsWith(" floor")).sharedMesh;var ground=floor.vertices;var shades=floor.colors;
            Assert.AreEqual(floor.vertexCount,shades.Length,"hall after hall has the same floor");
            Color Hall(float z){var sum=Color.clear;int n=0;for(int i=0;i<ground.Length;i++)if(Mathf.Abs(ground[i].x)<3&&Mathf.Abs(ground[i].z-z)<3){sum+=shades[i];n++;}return sum/Mathf.Max(1,n);}
            Assert.Greater(Mathf.Abs(Hall(32).grayscale-Hall(140).grayscale),.1f,"the cut corridor's floor is the Great Lantern's floor");
            var kit=All<MeshRenderer>().Where(r=>r.enabled&&r.GetComponent<TextMeshPro>()==null&&!r.name.EndsWith(" floor")&&!r.name.EndsWith(" cliffs")).ToArray();
            foreach(var lamp in All<Light>().Where(l=>l.name=="Lamp light"))
            {
                var at=new Vector2(lamp.transform.position.x,lamp.transform.position.z);
                Assert.IsTrue(kit.Any(r=>Vector2.Distance(at,new Vector2(r.bounds.center.x,r.bounds.center.z))<Mathf.Max(1.5f,r.bounds.extents.magnitude)),$"a pool of light at {at} has no lamp");
            }
            int racks=All<Transform>().Count(t=>t.name=="DressShelfRack"),mirrors=All<Transform>().Count(t=>t.name=="RelayMirror");
            Assert.Greater(racks,0,"the vault has nothing of its own");Assert.Greater(mirrors,5,"the gallery and the lantern floor have nothing of their own");
        }

        [UnityTest] public IEnumerator TheHallsMeetOnALevelFloor()
        {
            yield return Zone("SunkenRelay");
            var ground=All<MeshCollider>().Single(c=>c.GetComponent<Renderer>()!=null);
            foreach(float z in new[]{20f,44f,84f,116f})for(float x=-3;x<=3;x+=1.5f)
            {
                Assert.IsTrue(ground.Raycast(new Ray(new Vector3(x,8,z),Vector3.down),out var hit,16),$"fixture: floor at ({x}, {z})");
                Assert.Less(hit.point.y,.12f,$"a ridge {hit.point.y:0.00} m high runs across the doorway at z {z}: from the camera it's a pale straight seam across the floor");
            }
            // And nothing the walls were placed by has floated up onto them.
            foreach(var column in All<Transform>().Where(t=>t.name=="AncelaneColumn"&&t.parent==null))
                Assert.Less(BoundsOf(column).min.y,.3f,$"the column at ({column.position.x:0}, {column.position.z:0}) stands on top of a wall, with half of it hanging over the hall");
        }

        // ---------------------------------------------------------------- plates and the cradle (item 17)
        [UnityTest] public IEnumerator ThePlatesStandClearAndTheCradleIsSigned()
        {
            yield return Zone("SunkenRelay");
            var parts=All<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<TextMeshPro>()==null&&!r.name.EndsWith(" floor")&&!r.name.EndsWith(" cliffs")&&r.name!="Sign plate").ToArray();
            foreach(var stand in All<Transform>().Where(t=>t.name=="Record plate stand"))
            {
                var at=new Vector2(stand.position.x,stand.position.z);
                foreach(var part in parts)
                {
                    if(part.transform.IsChildOf(stand)||part.GetComponentInParent<DiscoveryPoint>()!=null)continue;
                    var box=part.bounds;var nearest=new Vector2(Mathf.Clamp(at.x,box.min.x,box.max.x),Mathf.Clamp(at.y,box.min.z,box.max.z));
                    if(box.max.y<.2f||part.transform.root.name.StartsWith("Record plate"))continue;
                    Assert.Greater(Vector2.Distance(at,nearest),1.6f,$"the record plate's stand at {at} is lost in a pile with {part.transform.root.name}");
                    if(part.sharedMaterial!=null&&part.sharedMaterial.name.StartsWith("CrystalCluster"))
                        Assert.Greater(Vector2.Distance(at,nearest),4,"a loud crystal beside the plate draws the eye instead of it");
                }
            }
            var sign=All<TextMeshPro>().Single(t=>t.text=="RELAY CRADLE").transform.position;
            foreach(var rig in All<Transform>().Where(t=>t.name=="MusterCutterRig"))
                Assert.IsFalse(Hides(BoundsOf(rig),sign),"RELAY CRADLE lies behind the cutter rig's arm");
        }

        // ---------------------------------------------------------------- conversations (item 11)
        [UnityTest] public IEnumerator ThePartnerComesOverForAConversationAndEveryoneTurns()
        {
            yield return Zone("TallowDrift");
            var keeper=All<Npc>().Single(n=>n.speaker=="Keeper");var partner=PartyController.Current.members.First(m=>m!=Lead);
            partner.GetComponent<PartnerBrain>().enabled=true;
            var rest=keeper.transform.rotation;
            Put(Lead,keeper.transform.position+new Vector3(-2.4f,.1f,0));Put(partner,new Vector3(-9,.1f,-3));
            yield return new WaitForSecondsRealtime(.3f);
            DialogueSystem.Current.AutoAdvance=false;keeper.Interact();
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"fixture: the talk starts");
            deadline=Time.unscaledTime+5;while(Vector3.Distance(partner.transform.position,Lead.transform.position)>2.6f&&Time.unscaledTime<deadline)yield return null;
            // In range isn't arrived: she walks on to her mark for up to four seconds, then turns.
            yield return new WaitForSecondsRealtime(4.6f);
            Assert.Less(Vector3.Distance(partner.transform.position,Lead.transform.position),2.6f,"the partner stays across the room while the conversation she's part of goes on");
            float Facing(Transform who,Vector3 at){var to=at-who.position;to.y=0;return Vector3.Angle(who.forward,to);}
            Assert.Less(Facing(partner.transform,keeper.transform.position),40,"the partner stands with her back to whoever's talking");
            Assert.Less(Facing(Lead.transform,keeper.transform.position),30,"the hero looks away from the person he's talking to");
            Assert.Less(Facing(keeper.transform,Lead.transform.position),30,"the speaker talks past the hero");
            Assert.AreEqual(1,All<Npc>().Count(n=>n.speaker=="Keeper"),"fixture: one keeper");
            DialogueSystem.Current.Cancel();
            deadline=Time.unscaledTime+5;while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"fixture: the talk ends");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(Quaternion.Angle(rest,keeper.transform.rotation),3,"the speaker never turns back to their post");
        }
        [UnityTest] public IEnumerator TheFirstShopkeeperStandsAtAStallAndLooksAtYou()
        {
            yield return Zone("Hub_Decks");
            var mira=All<Npc>().Single(n=>n.speaker=="Mira");var at=mira.transform.position;
            Assert.IsNotNull(mira.GetComponent<ScriptedWalk>(),"fixture: she keeps her opening walk, finished or not");
            float nearest=All<Transform>().Where(t=>t.name=="DressMarketStall"&&t.gameObject.activeInHierarchy).Min(t=>new Vector2(t.position.x-at.x,t.position.z-at.z).magnitude);
            Assert.Less(nearest,2.6f,"Mira stands on the walkway runner away from any stall: at the game's first shop she doesn't read as a shopkeeper");
            Put(Lead,at+new Vector3(.4f,.1f,-2.2f));
            mira.transform.rotation=Quaternion.Euler(0,20,0);
            yield return new WaitForSecondsRealtime(.3f);
            DialogueSystem.Current.AutoAdvance=false;mira.Interact();
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"fixture: the talk starts");
            yield return new WaitForSecondsRealtime(.9f);
            var to=Lead.transform.position-mira.transform.position;to.y=0;
            Assert.Less(Vector3.Angle(mira.transform.forward,to),30,"Mira keeps her back to Taren through her whole conversation, smiling portrait and all");
            DialogueSystem.Current.Cancel();
        }
        [UnityTest] public IEnumerator TheRelayDoorOpensOnTheDark()
        {
            yield return Zone("LanternGorge");
            var arch=BoundsOf(WarpTo("SunkenRelay"));
            var way=All<Transform>().FirstOrDefault(t=>t.name=="Relay doorway");
            Assert.IsNotNull(way,"'Enter the Sunken Relay' is an arch standing against a cliff, with nothing there to enter");
            var view=way.GetComponentInChildren<Renderer>();var look=new MaterialPropertyBlock();view.GetPropertyBlock(look);
            Assert.Less(look.GetColor("_BaseColor").grayscale,.1f,"the way down through the door is as bright as the wall outside it");
            Assert.Less(((Vector4)look.GetVector("_EmissionColor")).magnitude,.01f,"the dark behind the door has a panel's cyan lights burning in it");
            var box=view.bounds;
            Assert.IsTrue(box.min.x>arch.min.x&&box.max.x<arch.max.x&&box.max.y<arch.max.y,"the dark pokes out round the arch it's meant to fill");
            Assert.IsTrue(box.center.z>arch.center.z&&box.min.z<arch.max.z+.5f,"the dark stands clear of the arch, with lit cliff between them");
            var floor=All<MeshFilter>().Single(f=>f.name.EndsWith(" floor")).sharedMesh;var points=floor.vertices;var tones=floor.colors;
            Assert.AreEqual(points.Length,tones.Length,"the floor runs lit right up to the dark and stops at a line");
            Color Tone(float x,float z){int best=0;float near=float.MaxValue;for(int i=0;i<points.Length;i++){float d=(points[i].x-x)*(points[i].x-x)+(points[i].z-z)*(points[i].z-z);if(d<near){near=d;best=i;}}return tones[best];}
            Assert.Less(Tone(0,232).grayscale,.4f,"the floor under the arch is as bright as the road up to it");
            Assert.Greater(Tone(0,222).grayscale,.8f,"the dark has run down the road, well clear of the door");
            Put(Lead,new Vector3(0,1,228.6f));yield return new WaitForSecondsRealtime(.9f);
            // Its lower part: from the prompt the top of an 8 m door is off the screen anyway.
            Assert.IsTrue(Inside(Camera.main.WorldToViewportPoint(new Vector3(box.center.x,box.min.y+1.5f,box.center.z))),"standing at the door's prompt, the dark it opens on is off the screen");
        }
        [UnityTest] public IEnumerator WhatReachesTheLensGoesWhole()
        {
            yield return Zone("LanternGorge");ClearFights();
            int read=0;
            foreach(var tower in All<SceneryOcclusion>().Where(s=>s.name.StartsWith("Gorge relay")).ToArray())
            {
                var over=BoundsOf(tower).center-SightlineAudit.ToCamera*SightlineAudit.Distance;
                Put(Lead,new Vector3(over.x,1,over.z));yield return new WaitForSecondsRealtime(1.2f);
                var box=BoundsOf(tower);var eye=Camera.main.transform.position;
                if(box.SqrDistance(eye)>=64||box.max.y<=eye.y-4)continue;
                read++;
                // 1 leaves dots where the depth is short of it, and always leaves the shadow; the shadow pass clears its last cell at 1.25.
                Assert.Greater(Faded(tower),1.2f,$"{tower.name} passing under the camera stays as a huge dotted silhouette over the floor, or goes and leaves its shadow behind, where the party walks and fights");
            }
            Assert.Greater(read,0,"fixture: the lens comes over at least one of the gorge's towers");
        }
        [UnityTest] public IEnumerator TheHoleForTheHeroIsACleanWindow()
        {
            yield return Zone("Lanternwick");
            var arch=All<SceneryOcclusion>().Single(s=>s.name=="Market arch");
            // Behind a leg, where it really does stand between the camera and the hero.
            Put(Lead,new Vector3(arch.transform.position.x+3.4f,.2f,arch.transform.position.z+3));
            yield return new WaitForSecondsRealtime(.8f);
            var block=new MaterialPropertyBlock();arch.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);
            float fade=block.GetFloat("_OccFade"),depth=block.GetFloat("_OccFadeDepth");
            Assert.Greater(fade,.9f,"fixture: the arch opens for the hero behind its leg");
            Assert.LessOrEqual(fade,1.001f,"the arch goes altogether for a hero behind one leg, and takes its shadow with it");
            // The last of the screen door's cells stands at .96875.
            Assert.Greater(fade*depth,.96875f,"the hero stands in a bubble of dots, the arch's bricks stippled across them: it doesn't read as seeing through");
        }
        [UnityTest] public IEnumerator TheWaysOutAreSignedClearOfTheHud()
        {
            foreach(var (zone,scene,spots) in new[]{
                ("LanternGorge","Lanternwick",new[]{new Vector3(0,.2f,0),new Vector3(-1.6f,.2f,-6)}),
                ("Lanternwick","LanternGorge",new[]{new Vector3(-31.5f,1.6f,84)}),
                ("LanternGorge","SunkenRelay",new[]{new Vector3(0,.3f,226.8f)}),
                ("SunkenRelay","LanternGorge",new[]{new Vector3(0,.1f,3)})})
            {
                yield return Zone(zone);ClearFights();
                var warp=WarpTo(scene);var sign=All<TextMeshPro>().Single(t=>t.text==warp.prompt&&t.GetComponentInParent<Npc>()==null);
                foreach(var spot in spots)
                {
                    Put(Lead,spot);yield return new WaitForSecondsRealtime(.8f);
                    foreach(var corner in Corners(sign))
                    {
                        var view=Camera.main.WorldToViewportPoint(corner);
                        foreach(var panel in HudPanels)Assert.IsFalse(panel.Contains(view),$"{zone}, at {spot}: \"{sign.text}\" runs behind the HUD at {view}: it loses its first words");
                    }
                }
            }
        }
        [UnityTest] public IEnumerator FloorAndWallMeetInShade([Values("Lanternwick","LanternGorge","SunkenRelay")] string zone)
        {
            yield return Zone(zone);
            var floor=All<MeshFilter>().Single(f=>f.name.EndsWith(" floor")).sharedMesh;var walls=All<MeshFilter>().Single(f=>f.name.EndsWith(" cliffs")).sharedMesh;
            var floorAt=floor.vertices;var floorTone=floor.colors;var wallAt=walls.vertices;var wallTone=walls.colors;
            Assert.AreEqual(floorAt.Length,floorTone.Length,$"{zone}: pale ground runs right up to the strata at full contrast, a row of hard triangular teeth along every wall");
            Assert.AreEqual(wallAt.Length,wallTone.Length,$"{zone}: the strata start at full brightness from the floor, a row of hard triangular teeth along every wall");
            Vector2Int Key(Vector3 p)=>new(Mathf.RoundToInt(p.x*4),Mathf.RoundToInt(p.z*4));
            var onFloor=new HashSet<Vector2Int>();foreach(int i in floor.triangles)onFloor.Add(Key(floorAt[i]));
            int meet=0,shaded=0;
            for(int i=0;i<wallAt.Length;i++)if(onFloor.Contains(Key(wallAt[i]))){meet++;if(wallTone[i].grayscale<.75f)shaded++;}
            Assert.Greater(meet,100,$"fixture: {zone}'s floor meets its walls");
            // Not all: where a wall's top meets a higher floor, as along the lantern terrace, nothing rises above it to throw shade.
            Assert.Greater(shaded,meet*.6f,$"{zone}: only {shaded} of the {meet} points where floor meets wall are in shade, so the saw edge between the two textures reads at full contrast");
        }
    }
}
