using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The eye pass on chapter 1's places, and the sign style the whole game shares. Readers looked at a full playthrough; these pin what they
    /// saw to the cause. Whether it now looks finished is still a question for eyes on the next capture.</summary>
    public sealed class QaChapterOneLookTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y;return Vector3.Distance(a,b);}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            if(PartyController.Current!=null)foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();
        }

        // ---------------------------------------------------------------- what a sign is
        static TextMeshPro[] Signs()
        {
            var people=new HashSet<string>(All<Npc>().Select(n=>DisplayNames.Speaker(n.speaker)));
            return All<TextMeshPro>().Where(t=>t.name.StartsWith("Sign_")&&t.text.Length>0&&!people.Contains(t.text)&&t.GetComponentInParent<Npc>(true)==null).ToArray();
        }
        static bool Warp(TextMeshPro sign)=>sign.text.StartsWith("Warp")||All<WarpBeacon>().Any(w=>w.prompt.StartsWith(sign.text));
        static bool OnTheFloor(TextMeshPro sign)=>!Warp(sign)&&(!ZoneController.Current.Flight||sign.transform.position.y<1);
        static CameraProfile View=>ZoneController.Current.definition.cameraProfile;
        static float Pixels(TextMeshPro sign)=>sign.fontSize*.1f*sign.transform.lossyScale.y/(2*View.distance*Mathf.Tan(View.fov*.5f*Mathf.Deg2Rad))*1080;
        /// <summary>Points across what the sign paints: its words and their margin, in the world.</summary>
        static IEnumerable<Vector3> Paint(TextMeshPro sign)
        {
            var size=sign.GetPreferredValues(sign.text);float w=size.x+.3f,h=size.y;
            for(int i=0;i<=8;i++)for(int j=0;j<=2;j++)yield return sign.transform.TransformPoint(new Vector3((i/8f-.5f)*w,(j/2f-.5f)*h,0));
        }
        /// <summary>What a sign can run into: anything standing, drawn and not a person. Flat paint and the ground itself don't count, a slab's carving does.</summary>
        static bool Stands(Renderer part,float floor)
        {
            if(part is not MeshRenderer||!part.enabled||!part.gameObject.activeInHierarchy)return false;
            if(part.GetComponentInParent<TMP_Text>()!=null||part.GetComponentInParent<CombatActor>()!=null||part.GetComponentInParent<Npc>()!=null||part.GetComponent<BiomePatch>()!=null)return false;
            if(part.name.StartsWith("Floor detail")||part.name.StartsWith("Lit chunk")||part.name=="Walkway runner"||part.name=="Rug")return false;
            var box=part.bounds;if(Mathf.Max(box.size.x,box.size.z)>40)return false;
            for(var up=part.transform;up!=null;up=up.parent)if(up.name=="HushwellMarker")return true;
            return box.max.y>floor+.25f;
        }

        // ---------------------------------------------------------------- the sign style
        [UnityTest] public IEnumerator FloorSignsReadFromTheFixedCameraOnAnyFloor()
        {
            var failures=new List<string>();Material shared=null;int read=0;
            foreach(var zone in new[]{"Hub_Decks","Sorrel_Ridges","Hushwell","Gullet_Tunnel","TallowDrift","Lanternwick","LanternGorge","SunkenRelay"})
            {
                yield return Zone(zone);
                foreach(var sign in Signs().Where(OnTheFloor))
                {
                    read++;string name=zone+" \""+sign.text+"\"";
                    if(Pixels(sign)<36)failures.Add($"{name} stands {Pixels(sign):0} px tall: readers measured letters of 8 to 12 px that nobody can read from a couch");
                    if(sign.gameObject.activeInHierarchy&&Physics.Raycast(sign.transform.position+Vector3.up*.3f,Vector3.down,out var under,8,~0,QueryTriggerInteraction.Ignore))
                    {
                        // A level sign can't lie along the Gullet's curved bed: there its low end rests on it, and TheEddysSignLiesOnTheEddysFloor holds the worst case.
                        if(!ZoneController.Current.Flight&&sign.transform.position.y-under.point.y>.6f)failures.Add($"{name} hangs {sign.transform.position.y-under.point.y:0.0} m over the floor it names");
                        if(sign.transform.TransformPoint(0,-sign.GetPreferredValues(sign.text).y*.5f,0).y<under.point.y-.02f)failures.Add($"{name} is half sunk: the bottom of its letters goes under the floor");
                    }
                    var face=sign.fontSharedMaterial;shared??=face;
                    if(face!=shared)failures.Add($"{name} has a material of its own: every sign should share one");
                    if(face.GetFloat("_FaceDilate")<.15f)failures.Add($"{name} keeps Cinzel's thin strokes, which vanish on a light floor");
                    if(face.GetFloat("_OutlineWidth")<.25f||face.GetColor("_OutlineColor").maxColorComponent>.15f)failures.Add($"{name} has no strong dark edge to hold it on a bright floor");
                    var plate=sign.transform.Find("Sign plate");
                    if(plate==null){failures.Add($"{name} has no painted backing: pale type on Sorrel's orange ground is a hairline ghost");continue;}
                    var paint=plate.GetComponent<Renderer>().sharedMaterial;
                    if(paint.GetColor("_BaseColor").a<.4f||paint.GetColor("_BaseColor").maxColorComponent>.15f)failures.Add($"{name}'s backing is too faint or too pale to lift its words off the floor");
                }
            }
            Assert.Greater(read,15,"fixture: the zones carry floor signs");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator EverySignReadsLevelToTheCamera()
        {
            var failures=new List<string>();int warps=0;
            foreach(var zone in new[]{"Hub_Decks","Hub_CinderHalo","Sorrel_Ridges","Hushwell","Gullet_Tunnel","TallowApproach","TallowDrift","WickReach","Lanternwick","LanternGorge","SunkenRelay"})
            {
                yield return Zone(zone);
                var level=Quaternion.Euler(0,View.yaw,0)*Vector3.right;var look=Quaternion.Euler(View.pitch,View.yaw,0)*Vector3.forward;
                foreach(var sign in Signs())
                {
                    float slant=Vector3.Angle(sign.transform.right,level);
                    if(slant>1)failures.Add($"{zone} \"{sign.text}\" slants {slant:0} degrees across the frame: the ground camera has a yaw of {View.yaw}");
                    if(Vector3.Dot(sign.transform.forward,look)<.6f)failures.Add($"{zone} \"{sign.text}\" doesn't face the camera");
                    if(!Warp(sign))continue;
                    warps++;float px=Pixels(sign);
                    if(px<24||px>34)failures.Add($"{zone} \"{sign.text}\" stands {px:0} px tall: a warp's sign should read at the type floor or a little over, and not run into the bottom HUD");
                }
            }
            Assert.Greater(warps,8,"fixture: the warps carry signs");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>A prop's world box is good enough for a crate. A long curved wall's box reaches far past the wall itself, so a big piece is read by its real mesh.</summary>
        static bool Meets(Renderer part,Ray ray,float reach)
        {
            if(!part.bounds.IntersectRay(ray,out float along)||along>=reach)return false;
            if(part.bounds.extents.magnitude<8||!part.TryGetComponent<MeshFilter>(out var shape)||shape.sharedMesh==null)return true;
            var probe=part.gameObject.AddComponent<MeshCollider>();probe.sharedMesh=shape.sharedMesh;Physics.SyncTransforms();
            bool met=probe.Raycast(ray,out _,reach);Object.DestroyImmediate(probe);return met;
        }
        [UnityTest] public IEnumerator FloorSignsStandClearOfPropsAndEachOther()
        {
            var failures=new List<string>();int read=0;
            foreach(var zone in new[]{"Hub_Decks","Sorrel_Ridges","Hushwell","Gullet_Tunnel"})
            {
                yield return Zone(zone);
                var rotation=Quaternion.Euler(View.pitch,View.yaw,0);var look=rotation*Vector3.forward;var right=rotation*Vector3.right;var up=rotation*Vector3.up;
                var signs=Signs().Where(s=>OnTheFloor(s)&&s.gameObject.activeInHierarchy).ToArray();var parts=All<Renderer>();
                var seen=new Dictionary<TextMeshPro,Rect>();
                foreach(var sign in signs)
                {
                    read++;float floor=sign.transform.position.y-.2f;
                    var standing=parts.Where(p=>Stands(p,floor)&&Vector3.Distance(p.bounds.center,sign.transform.position)<40+p.bounds.extents.magnitude).ToArray();
                    var hit=new HashSet<string>();float xMin=1e6f,xMax=-1e6f,yMin=1e6f,yMax=-1e6f;
                    foreach(var point in Paint(sign))
                    {
                        float x=Vector3.Dot(point,right),y=Vector3.Dot(point,up);xMin=Mathf.Min(xMin,x);xMax=Mathf.Max(xMax,x);yMin=Mathf.Min(yMin,y);yMax=Mathf.Max(yMax,y);
                        // From the camera to the sign, and a little past it: in front of the words hides them, right behind them is printed on them.
                        var ray=new Ray(point-look*View.distance,look);
                        foreach(var part in standing)if(Meets(part,ray,View.distance+1.2f))hit.Add(part.transform.root.name+"/"+part.name+" at "+part.bounds.center+" size "+part.bounds.size+" sign "+point);
                    }
                    seen[sign]=Rect.MinMaxRect(xMin,yMin,xMax,yMax);
                    if(hit.Count>0)failures.Add($"{zone} \"{sign.text}\" runs into {string.Join(", ",hit)}");
                }
                foreach(var a in signs)foreach(var b in signs)if(a.GetInstanceID()<b.GetInstanceID()&&seen[a].Overlaps(seen[b]))failures.Add($"{zone} \"{a.text}\" crosses \"{b.text}\"");
            }
            Assert.Greater(read,12,"fixture: the zones carry floor signs");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator SignsStepAsideWhileABossBarIsUp()
        {
            yield return Zone("Hushwell");
            var sign=All<TextMeshPro>().First(t=>t.text=="BELLOWS CHAMBER");
            var lead=PartyController.Current.Active;
            // Past the sign but short of the arena's trigger (z 249 to 275): standing inside it starts the fight by itself, and the sign would already be going.
            foreach(var hero in PartyController.Current.members){Put(hero,new Vector3(hero==lead?7:-7,HushwellLayout.Middle+.1f,247));hero.Health.maximum=hero.Health.integrity=90000;}
            yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(sign.alpha,.9f,"fixture: nobody stands near the sign, so it's showing");
            All<EncounterVolume>().Single(e=>e.encounterId=="Hushwell_Bellows").Begin();
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsNotNull(BossController.Focus(lead.transform.position),"fixture: the boss bar is up");
            Assert.Less(sign.alpha,.1f,"BELLOWS CHAMBER stays on the floor right under the boss bar and runs through its words");
            var plate=sign.transform.Find("Sign plate");Assert.IsNotNull(plate,"the sign has no backing to fade with it");
            var block=new MaterialPropertyBlock();plate.GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.IsTrue(block.HasFloat("_Fade"),"the sign's words step aside and leave their dark plate behind: nothing ever told it to fade");
            Assert.Less(block.GetFloat("_Fade"),.1f,"the sign's words step aside and leave their dark plate behind");
        }
        [UnityTest] public IEnumerator ASignStaysReadableUntilTheHeroIsOnIt()
        {
            yield return Zone("Hushwell");
            var sign=All<TextMeshPro>().First(t=>t.text=="BELLOWS CHAMBER");
            Put(PartyController.Current.Active,sign.transform.position+new Vector3(0,.1f,-5));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(sign.alpha,.9f,"a sign five metres from the hero is already half faded, so on a screen that follows the hero it's faint for most of the time it's in view");
        }

        // ---------------------------------------------------------------- Sorrel
        [UnityTest] public IEnumerator TheDrillSealsAreAHazardLineNotAPlank()
        {
            yield return Zone("Sorrel_Ridges");
            foreach(var name in new[]{"Drill entry seal","Drill cradle seal"})
            {
                var seal=All<Membrane>().Single(m=>m.name==name);
                seal.SetOpen(false);yield return null;
                var parts=seal.GetComponentsInChildren<Renderer>(true);
                Assert.IsNotEmpty(parts,name+" closes with nothing to see");
                foreach(var part in parts)
                {
                    Assert.IsInstanceOf<LineRenderer>(part,name+" is still a stretched block lying on the dirt, not a line of light");
                    StringAssert.DoesNotContain("gullet-membrane",part.sharedMaterial.name,name+" wears the Gullet's flesh at a dusty drill site");
                }
                foreach(var line in seal.GetComponentsInChildren<LineRenderer>())
                {
                    // Measured from the floor under each end: the line rises with the slope at the arena's edge.
                    foreach(int end in new[]{0,1})
                    {
                        var at=line.useWorldSpace?line.GetPosition(end):line.transform.TransformPoint(line.GetPosition(end));
                        Assert.IsTrue(Physics.Raycast(at+Vector3.up*.2f,Vector3.down,out var ground,6,~0,QueryTriggerInteraction.Ignore),name+"'s line hangs over nothing");
                        Assert.Less(at.y-ground.point.y,1.1f,name+"'s light stands above a hero's waist");
                    }
                    Assert.Less(line.startWidth,.3f,name+" is a band across the arena, not a line");
                }
                var box=seal.GetComponent<BoxCollider>();Assert.IsNotNull(box,name+" lost the collider that holds the fight in");
                Assert.AreEqual(26,box.size.x*seal.transform.lossyScale.x,.1f,name+"'s collider changed: only its look should have");
                Assert.IsTrue(box.enabled,name+" is shut but solid no more");
                seal.SetOpen(true);yield return new WaitForSecondsRealtime(.7f);
                Assert.IsFalse(box.enabled,name+" stays solid once it's open");
                Assert.IsTrue(seal.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled),name+"'s light stays on once the way is open");
            }
            Assert.GreaterOrEqual(All<Transform>().Count(t=>t.name=="Seal post"),10,"nothing stands along the line to say a barrier closes here");
            Assert.IsEmpty(All<Transform>().Where(t=>t.name=="Seal post").SelectMany(t=>t.GetComponentsInChildren<Collider>()),"a post can trap someone the site's navigation doesn't know about");
        }
        [UnityTest] public IEnumerator SorrelsCliffsTakeTheirTextureFromTheSide()
        {
            yield return Zone("Sorrel_Ridges");
            var ground=GameObject.Find("Continuous eroded basin").GetComponent<Renderer>().sharedMaterial;
            Assert.IsTrue(ground.IsKeywordEnabled("_TRIPLANAR_ON"),"the slope wears the floor's plan projection: one row of texels smeared down a cliff, long diagonal streaks like motion blur");
            Assert.Greater(ground.GetFloat("_TriplanarScale"),.05f,"the side projection is scaled away to nothing");
            Assert.IsTrue(ground.IsKeywordEnabled("_STRATA_ON"),"the cliffs lost their horizontal strata");
        }
        [UnityTest] public IEnumerator TheKeySitsInAMachineThatStaysWhenItGoes()
        {
            Flag("bossdown.Burrower");
            yield return Zone("Sorrel_Ridges");
            var key=All<KeyPickup>().Single();
            Assert.IsFalse(key.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("LatticeAnvil")),"the drill's cradle is a second Lattice Anvil, a machine the player already knows as the fabrication bench");
            Assert.IsNotNull(key.visual,"nothing at the cradle is the key: there's a prompt and no key in sight");
            Assert.IsTrue(key.visual.activeInHierarchy,"the key isn't on show");
            Assert.IsTrue(key.visual.transform.IsChildOf(key.transform),"fixture: the key sits in its cradle");
            var view=key.visual.GetComponentInChildren<Renderer>();Assert.IsNotNull(view,"the key has no body");
            Assert.Less(view.bounds.size.magnitude,1.6f,"the key is bigger than a hero's torso");
            Assert.Greater(view.sharedMaterial.GetColor("_EmissionColor").maxColorComponent,1,"the key doesn't glow: nothing draws the eye to it");
            bool Machine(Renderer r)=>!r.transform.IsChildOf(key.visual.transform);
            Assert.GreaterOrEqual(key.GetComponentsInChildren<Renderer>().Count(Machine),2,"there's no machine for the key to sit in");
            key.Interact();
            Assert.IsTrue(Has("warpkey"),"fixture: the key was taken");
            Assert.IsFalse(key.visual.activeSelf,"the key is still there after it's been taken");
            Assert.IsTrue(key.gameObject.activeInHierarchy,"the whole machine, far bigger than Taren, vanishes with the key");
            Assert.GreaterOrEqual(key.GetComponentsInChildren<Renderer>().Count(Machine),2,"the machine goes with the key");
            yield return Zone("Sorrel_Ridges");
            key=All<KeyPickup>().Single();
            Assert.IsFalse(key.visual.activeSelf,"the key is back in its cradle on a later visit");
            Assert.IsTrue(key.gameObject.activeInHierarchy,"the machine is gone on a later visit");
        }
        [UnityTest] public IEnumerator SorrelsPlantsDoNotReadAsCrystals()
        {
            yield return Zone("Sorrel_Ridges");
            var plant=All<Transform>().First(t=>t.name=="DressSucculent").GetComponentInChildren<Renderer>();
            var material=plant.sharedMaterial;
            var red=new Color(.85f,.3f,.25f);var shown=Color.Lerp(Color.white,red,material.GetFloat("_BaseMapStrength"))*material.GetColor("_BaseColor");
            Assert.Less(shown.r/Mathf.Max(.01f,shown.g),1.35f,"a red faceted clump beside the road is what a crystal looks like, while 'Also: Mine Ridge Crystals' points elsewhere");
        }
        static bool IsPlant(Transform part)
        {
            for(var up=part;up!=null;up=up.parent)if(up.name=="DressSucculent"||up.name=="DressFanShrub")return true;
            return false;
        }
        [UnityTest] public IEnumerator PlantsGrowOnOpenGroundNotOutOfPadsAndProps()
        {
            yield return Zone("Sorrel_Ridges");
            var plants=All<Transform>().Where(t=>IsPlant(t)&&t.parent!=null&&t.parent.name=="Room dressing").ToArray();
            Assert.Greater(plants.Length,100,"fixture: Sorrel's basin has its scrub");
            var built=All<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponentInParent<TMP_Text>()==null&&r.GetComponent<BiomePatch>()==null&&r.GetComponentInParent<CombatActor>()==null&&r.GetComponentInParent<Npc>()==null
                &&!r.name.StartsWith("Floor detail")&&!IsPlant(r.transform)&&Mathf.Max(r.bounds.size.x,r.bounds.size.z)<60).Select(r=>(box:r.bounds,name:r.transform.parent!=null?r.transform.parent.name:r.name)).ToArray();
            var clipping=new List<string>();
            foreach(var plant in plants)foreach(var (box,name) in built)
            {
                var at=plant.position;
                if(at.x>box.min.x-.3f&&at.x<box.max.x+.3f&&at.z>box.min.z-.3f&&at.z<box.max.z+.3f){clipping.Add($"{plant.name} at ({at.x:0.0},{at.z:0.0}) in {name}");break;}
            }
            Assert.IsEmpty(clipping,"plants come through steel pads, press into cart wheels and poke out from under the parked skiff, like random scatter in a place people built:\n"+string.Join("\n",clipping.Take(12)));
        }
        [UnityTest] public IEnumerator GlowingPropsKeepTheirDetail()
        {
            yield return Zone("Sorrel_Ridges");
            foreach(var name in new[]{"LatticeAnvil","HushwellOreCart"})
            {
                var glow=All<Transform>().First(t=>t.name==name).GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_EmissionColor").maxColorComponent;
                Assert.Less(glow,1,name+" glows past its own lighting: the anvil's top is a flat cyan rectangle and the cart's load a cyan blob, beside props that show their paint");
                Assert.Greater(glow,.2f,name+" stopped glowing: it should still catch the eye");
            }
        }
        [UnityTest] public IEnumerator GroundStampsFeatherIntoTheFloor()
        {
            yield return Zone("Sorrel_Ridges");
            Renderer Stamp(string kind)=>All<Renderer>().FirstOrDefault(r=>r.name=="Floor detail "+kind);
            var tracks=Stamp("GroundTracks");Assert.IsNotNull(tracks,"fixture: Sorrel has tyre tracks");
            Assert.IsTrue(tracks.sharedMaterial.IsKeywordEnabled("_VCOLOR_ON"),"tyre tracks start and stop on a hard square-cut line in open ground");
            foreach(var kind in new[]{"GroundClay","GroundGravel","GroundRipples"})
            {
                var stamp=Stamp(kind);Assert.IsNotNull(stamp,"fixture: Sorrel has "+kind);
                Assert.IsTrue(stamp.sharedMaterial.IsKeywordEnabled("_VCOLOR_ON"),kind+" has no feathered edge of its own: every patch shows the same sticker outline");
                Assert.LessOrEqual(stamp.sharedMaterial.GetColor("_BaseColor").a,.65f,kind+" sits on the ground as a saturated blob");
            }
            yield return Zone("Hushwell");
            foreach(var kind in new[]{"GroundDust","GroundBasalt","GroundFissure"})
            {
                var stamp=Stamp(kind);Assert.IsNotNull(stamp,"fixture: Hushwell has "+kind);
                Assert.IsTrue(stamp.sharedMaterial.IsKeywordEnabled("_VCOLOR_ON"),kind+" has no feathered edge of its own: a rounded-square footprint on the cave floor");
            }
            var dust=All<Renderer>().Where(r=>r.name=="Floor detail GroundDust").ToArray();
            // HEAD has 17 batches. The seeded stream, run offline against HEAD's real batches, predicts 11 at the new rates: 13 leaves room for a patch the offline run can't see refused.
            Assert.LessOrEqual(dust.Length,13,"the same cold grey sand patch keeps turning up, often two to a screen");
            var tone=dust[0].sharedMaterial.GetColor("_BaseColor");
            Assert.Less(tone.a,.45f,"the sand patch is a blob on the floor, not a dusting of it");
            Assert.Greater(tone.r,tone.b*1.25f,"the sand patch stays cold grey on a warm brown floor");
            Assert.GreaterOrEqual(All<Transform>().Count(t=>t.name=="Pool rim"),12,"the nursery's pools have no shore: a flat soft patch with no edge");
        }
        static void CradleIsAtHand(RepairBay bay,Vector3 stand)
        {
            Assert.GreaterOrEqual(bay.range,4,bay.name+" only answers within "+bay.range+" m, and a party can stop short of it and swing at the air");
            PromptService.FindInteraction(stand);
            Assert.AreEqual(bay,PromptService.Interaction,bay.name+"'s prompt doesn't reach the player standing beside it");
            Assert.IsNotEmpty(bay.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.GetColor("_EmissionColor").maxColorComponent>1.5f),bay.name+" reads as a shut crate: nothing on it is lit");
            var around=All<Transform>().Where(t=>t.name==bay.name+" bay").ToArray();Assert.AreEqual(1,around.Length,bay.name+" stands alone: no pad or light says what it's for");
            Assert.IsNotEmpty(around[0].GetComponentsInChildren<Light>(),bay.name+" has no glow of its own");
            Assert.IsNotEmpty(around[0].GetComponentsInChildren<LineRenderer>(),bay.name+" has no light to find it by");
            Assert.IsEmpty(around[0].GetComponentsInChildren<Collider>(),bay.name+"'s dressing is solid and the site's navigation doesn't know it");
            Assert.IsTrue(All<TextMeshPro>().Any(t=>t.text.Contains("REPAIR")&&Flat(t.transform.position,bay.transform.position)<6),bay.name+"'s sign doesn't say it repairs");
        }
        [UnityTest] public IEnumerator TheFreeRepairsSayWhatTheyAreAndReachThePlayer()
        {
            yield return Zone("Sorrel_Ridges");
            var bay=All<RepairBay>().Single(b=>b.name=="Drill site cradle");
            // Chapter 7 stops at (-1.2,142) with .8 m to spare, and pressed there for nothing.
            var stop=new Vector3(-1.2f,-.1f,142);var away=stop-bay.transform.position;away.y=0;
            CradleIsAtHand(bay,stop+away.normalized*.8f);
            yield return Zone("Hushwell");
            bay=All<RepairBay>().Single(b=>b.name=="Survey cradle");
            CradleIsAtHand(bay,bay.transform.position+new Vector3(-.5f,-.7f,-3.5f));
        }
        [UnityTest] public IEnumerator TheBoreHasAMouth()
        {
            yield return Zone("Sorrel_Ridges");
            var shaft=All<Transform>().FirstOrDefault(t=>t.name=="Bore shaft");
            Assert.IsNotNull(shaft,"the way down is a metal table over a brown stain: nothing you could descend");
            var dark=shaft.GetComponent<Renderer>();
            Assert.Less(dark.sharedMaterial.GetColor("_BaseColor").maxColorComponent,.08f,"the shaft isn't dark");
            Assert.Greater(dark.sharedMaterial.GetColor("_BaseColor").a,.8f,"you can see the dirt through the shaft");
            var bore=All<WarpBeacon>().Single(w=>w.scene=="Hushwell");
            Assert.Greater(dark.bounds.extents.x,1.8f,"the shaft is narrower than the scaffold over it");
            Assert.Less(Flat(dark.bounds.center,bore.transform.position),.5f,"the shaft isn't under the scaffold");
            var mouth=shaft.parent;
            Assert.GreaterOrEqual(mouth.Cast<Transform>().Count(t=>t.name.StartsWith("Bore rim")),4,"the shaft has no rim: a black disc on bare dirt");
            Assert.IsEmpty(mouth.GetComponentsInChildren<Collider>(),"the bore's dressing is solid and blocks the stance every route uses");
            PromptService.FindInteraction(new Vector3(8,-.1f,174.5f));
            Assert.AreEqual(bore,PromptService.Interaction,"the prompt no longer reaches where chapter 7 stands");
        }
        [UnityTest] public IEnumerator TheWreckIsDugInAndLeftAScar()
        {
            Flag("bossdown.Burrower");
            yield return Zone("Sorrel_Ridges");
            var wreck=All<Transform>().First(t=>t.name=="Burrower wreck");var rig=wreck.Find("Rig");
            Assert.Greater(Vector3.Dot(rig.forward,Vector3.down),.3f,"the rig lies level with its tail in the air like a spare drill bit, not dug in nose first");
            var low=rig.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);
            Assert.Less(low,wreck.position.y-.6f,"the rig rests on the ground instead of being driven into it");
            var parts=wreck.Find("Thrown parts");
            Assert.IsNotNull(parts.Cast<Transform>().FirstOrDefault(t=>t.name=="Wreck scar"),"nothing on the ground shows where it stopped");
            Assert.GreaterOrEqual(parts.childCount,5,"too little was thrown clear to say a machine broke here");
        }
        [UnityTest] public IEnumerator TheExcavationIsAWorkSite()
        {
            yield return Zone("Sorrel_Ridges");
            Assert.IsNotEmpty(All<Renderer>().Where(r=>r.name=="Floor detail DecalRails"&&r.bounds.center.z>150),"the excavation is more of the same flat dirt: no line runs from the bore to the hopper");
            var spoil=All<Transform>().Where(t=>t.name=="Excavation spoil").ToArray();
            Assert.GreaterOrEqual(spoil.Length,3,"nothing the drill threw up lies about the pit");
            Assert.IsEmpty(spoil.SelectMany(t=>t.GetComponentsInChildren<Collider>()),"spoil in the arena changes the fight's ground");
        }

        // ---------------------------------------------------------------- Hushwell
        [UnityTest] public IEnumerator TheBreachsLightsHaveLamps()
        {
            yield return Zone("Hushwell");
            var lamps=All<Transform>().Where(t=>t.name=="DressWorkLamp").ToArray();
            var lights=All<Light>().Where(l=>l.name=="Work light").ToArray();Assert.IsNotEmpty(lights,"fixture: the breach is lit");
            foreach(var light in lights)Assert.Less(lamps.Min(l=>Flat(l.position,light.transform.position)),2.2f,"a flat orange oval on the breach floor at "+light.transform.position+" has no lamp near it");
        }
        [UnityTest] public IEnumerator VentsBreatheSteamNotRings()
        {
            yield return Zone("Hushwell");
            var vent=All<PressurePulse>().First(p=>p.hazardRadius>0);
            float deadline=Time.unscaledTime+PressurePulse.Period+1;
            while(vent.Open&&Time.unscaledTime<deadline)yield return null;
            while(!vent.Open&&Time.unscaledTime<deadline)yield return null;
            yield return null;
            var near=All<ParticleSystem>().Where(p=>p.IsAlive()&&Vector3.Distance(p.transform.position,vent.transform.position)<4).ToArray();
            Assert.IsNotEmpty(near,"fixture: the exhale puffs");
            foreach(var puff in near)Assert.AreNotEqual("circle_02",puff.GetComponent<ParticleSystemRenderer>().sharedMaterial.name,"the vent breathes thin outline circles, flat to the camera: debug gizmos, not gas");
        }

        // ---------------------------------------------------------------- the Gullet
        [UnityTest] public IEnumerator TheEddysSignLiesOnTheEddysFloor()
        {
            yield return Zone("Gullet_Tunnel");
            var sign=All<TextMeshPro>().First(t=>t.text=="SALVAGE EDDY").transform;
            bool below=Physics.Raycast(sign.position+Vector3.up*2,Vector3.down,out var floor,20,~0,QueryTriggerInteraction.Ignore);
            Assert.IsTrue(below&&floor.collider.name=="Gullet membrane shell","SALVAGE EDDY hangs in the void off the floor");
            Assert.Less(sign.position.y-floor.point.y,.6f,"SALVAGE EDDY hangs metres over the floor");
            Assert.Less(Flat(sign.position,All<SalvageField>().Single(c=>c.cacheId=="Neve").transform.position),22,"the sign is nowhere near the eddy");
        }
        [UnityTest] public IEnumerator TheShellHasNoHardSeamAtItsValves()
        {
            yield return Zone("Gullet_Tunnel");
            var shell=GameObject.Find("Gullet membrane shell");
            Assert.AreEqual(1,shell.GetComponent<MeshRenderer>().sharedMaterials.Length,"the shell's organs are separate materials meeting in a hard saw-toothed line: from the flight camera the tube's cross-section reads as the rim of a huge flat disc");
            Assert.IsTrue(shell.GetComponent<MeshRenderer>().sharedMaterial.IsKeywordEnabled("_VCOLOR_ON"),"the shell's organs have nothing to fade across");
            var mesh=shell.GetComponent<MeshCollider>().sharedMesh;var v=mesh.vertices;var c=mesh.colors;
            var rows=Enumerable.Range(0,v.Length).GroupBy(i=>Mathf.RoundToInt(v[i].z*2)).OrderBy(g=>g.Key).Select(g=>(z:g.Key*.5f,glow:g.Average(i=>c[i].a))).ToArray();
            Assert.Greater(rows.Max(r=>r.glow)-rows.Min(r=>r.glow),.5f,"the organs have lost their own glow");
            for(int i=1;i<rows.Length;i++)Assert.Less(Mathf.Abs(rows[i].glow-rows[i-1].glow),.2f,"the shell's glow jumps at z="+rows[i].z);
        }
        [UnityTest] public IEnumerator VeinsAreFewAndQuiet()
        {
            yield return Zone("Gullet_Tunnel");
            var veins=All<Renderer>().Where(r=>r.name=="Floor detail GroundVeins").ToArray();
            Assert.IsNotEmpty(veins,"fixture: the Gullet has veins");
            Assert.LessOrEqual(veins.Length,60,"the same vein stamp repeats seven or eight times at once when the camera pulls back");
            var material=veins[0].sharedMaterial;
            Assert.Less(material.GetColor("_EmissionColor").maxColorComponent,.2f,"the floor's veins are brighter than the Chorister Darts flying over them");
            Assert.LessOrEqual(material.GetColor("_BaseColor").a,.6f,"the veins sit on the floor at full strength");
            Assert.IsTrue(material.IsKeywordEnabled("_VCOLOR_ON"),"every vein is the same stamp with the same outline");
        }
        [UnityTest] public IEnumerator TheEddysCacheIsNotAPaleBox()
        {
            yield return Zone("Gullet_Tunnel");
            var cache=All<SalvageField>().Single(c=>c.cacheId=="Neve");
            Assert.IsFalse(cache.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="DeckCrate"),"the eddy's cache is a clean deck crate with a black triangle on its lid, the brightest thing in a dim room");
            var view=cache.GetComponentsInChildren<Renderer>();Assert.IsNotEmpty(view,"the cache has no body");
            foreach(var part in view)Assert.Less(part.sharedMaterial.GetColor("_BaseColor").grayscale,.7f,"the cache is the brightest thing in a dim room");
            Assert.IsTrue(cache.GetComponent<Collider>().isTrigger,"the cache became something ships hit");
        }

        // ---------------------------------------------------------------- the Decks
        [UnityTest] public IEnumerator MiraWalksHomeAndTurnsToFaceTheRoom()
        {
            GameServices.Current.NewGame();Flag(OpeningMarketSequence.Requested);
            yield return Zone("Hub_Decks","Office");
            var mira=All<Npc>().Single(n=>n.speaker=="Mira");var walk=mira.GetComponent<ScriptedWalk>();
            Assert.AreEqual(ScriptedWalk.Stage.Arrived,walk.Phase,"fixture: Mira waits at her stall through the repair");
            walk.speed=14;Flag(OpeningMarketSequence.Repaired);
            float deadline=Time.unscaledTime+8;
            while(walk.Phase!=ScriptedWalk.Stage.Done&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(ScriptedWalk.Stage.Done,walk.Phase,"fixture: Mira gets home");
            yield return new WaitForSecondsRealtime(1f);
            Assert.Greater(Vector3.Dot(mira.transform.forward,Vector3.back),.9f,"Mira stops the way she last walked, with her back to the room and the camera, and holds her whole first conversation like that");
        }
        [UnityTest] public IEnumerator TheFirstJobShowsItsHoleAndPaintedGates()
        {
            GameServices.Current.NewGame();Flag(OpeningMarketSequence.Requested);
            yield return Zone("Hub_Decks","Office");
            var hole=All<Transform>().First(t=>t.name=="Walkway service opening").GetComponentInChildren<Renderer>().bounds;
            var runner=All<Renderer>().First(r=>r.name=="Walkway runner"&&r.bounds.min.x<hole.center.x&&r.bounds.max.x>hole.center.x&&r.bounds.min.z<hole.center.z&&r.bounds.max.z>hole.center.z);
            Assert.Greater(hole.max.y,runner.bounds.max.y+.005f,"the promenade's runner lies over the service opening, so the job is a black board standing on an unbroken walkway with no hole under it");
            var paint=All<Transform>().First(t=>t.name=="Safety gate panel").GetComponentInChildren<Renderer>().sharedMaterial.GetTexture("_BaseMap");
            Assert.IsTrue(paint!=null&&paint.width>16,"the safety gates are flat amber slabs with no paint at all, beside hand-painted walls");
        }

        // ---------------------------------------------------------------- every lamp in the game
        [UnityTest] public IEnumerator ALampsPoolHasNoHardRim()
        {
            yield return null;
            var shader=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Art/Shaders/LatticeToon.shader"));
            StringAssert.Contains("lighting += light.color",shader,"fixture: the toon shader adds its lamps here");
            StringAssert.DoesNotContain("ToonRamp(contribution)",shader,"a lamp's light passes through the toon ramp, whose first band is a flat disc with a hard rim on every floor in the game: painted spots with no falloff");
        }

        // ---------------------------------------------------------------- the Halo
        [UnityTest] public IEnumerator TheTenderIsATintedPodNotAGiantHelmet()
        {
            yield return Zone("Hub_CinderHalo","Outer");
            var bay=All<RepairBay>().Single(b=>b.name=="Repair tender");var warp=All<WarpBeacon>().Single(w=>w.scene=="Gullet_Tunnel");
            Bounds Box(Component c){var parts=c.GetComponentsInChildren<Renderer>();var box=parts[0].bounds;foreach(var p in parts)box.Encapsulate(p.bounds);return box;}
            var hull=Box(bay);
            Assert.Less(hull.size.y,Box(warp).size.y,"the tender stands larger than the warp beside it");
            foreach(var part in bay.GetComponentsInChildren<Renderer>())Assert.Less(part.sharedMaterial.GetColor("_BaseColor").grayscale,.7f,"the tender's shell is a blown-out cream under the Halo's sun");
            var sign=All<TextMeshPro>().First(t=>t.text=="REPAIR TENDER");
            var look=Quaternion.Euler(View.pitch,View.yaw,0)*Vector3.forward;
            foreach(var point in Paint(sign))Assert.IsFalse(hull.IntersectRay(new Ray(point-look*View.distance,look)),"the tender's sign touches its hull");
        }
    }
}
