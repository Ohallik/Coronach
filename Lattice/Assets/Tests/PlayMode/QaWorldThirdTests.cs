using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
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
    /// <summary>The third eye pass on the places themselves. A kit that vanishes with its job, a sign's plate that's a dirty rectangle on bright ground, brick stacks in open country, a first look at a town with no town in it,
    /// a stair that's a strip, a sunken hall lit like noon, a partner on the water. These pin what readers saw to its cause. Whether it now looks finished is for eyes on the next capture.</summary>
    public sealed class QaWorldThirdTests
    {
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
            if(PartyController.Current!=null)foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        static CombatActor Lead=>PartyController.Current.Active;
        static float Flat(Vector3 a,Vector3 b){a.y=b.y;return Vector3.Distance(a,b);}
        static bool Inside(Vector3 view)=>view.z>0&&view.x>.01f&&view.x<.99f&&view.y>.01f&&view.y<.99f;
        static Bounds BoundsOf(Component root)
        {
            var parts=root.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer or SkinnedMeshRenderer).ToArray();
            if(parts.Length==0)return new Bounds(root.transform.position,Vector3.zero);
            var box=parts[0].bounds;foreach(var part in parts)box.Encapsulate(part.bounds);return box;
        }
        static float Faded(SceneryOcclusion scenery)
        {
            var block=new MaterialPropertyBlock();float fade=0;
            foreach(var r in scenery.GetComponentsInChildren<Renderer>()){r.GetPropertyBlock(block);fade=Mathf.Max(fade,block.GetFloat("_OccFade"));}
            return fade;
        }
        static Color[] Read(Texture texture)
        {
            int width=Mathf.Min(texture.width,256),height=Mathf.Min(texture.height,256);
            var rt=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var image=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try{Graphics.Blit(texture,rt);RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);return image.GetPixels();}
            finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.Destroy(image);}
        }
        static float Median(List<float> values){values.Sort();return values.Count==0?0:values[values.Count/2];}
        static float ToSegment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        /// <summary>What share of the view a box takes, clipped to the screen, and first to what's in front of the lens: a corner behind the camera projects to anywhere, and a home it had already passed read as filling the screen.</summary>
        static float ScreenShare(Bounds box)
        {
            const float Near=.3f;var camera=Camera.main;var lens=camera.transform;
            var corners=new Vector3[8];var depth=new float[8];float xMin=1,xMax=0,yMin=1,yMax=0;
            for(int i=0;i<8;i++){corners[i]=box.center+Vector3.Scale(box.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));depth[i]=Vector3.Dot(corners[i]-lens.position,lens.forward);}
            void Take(Vector3 at){var view=camera.WorldToViewportPoint(at);xMin=Mathf.Min(xMin,view.x);xMax=Mathf.Max(xMax,view.x);yMin=Mathf.Min(yMin,view.y);yMax=Mathf.Max(yMax,view.y);}
            for(int i=0;i<8;i++)
            {
                if(depth[i]>Near)Take(corners[i]);
                foreach(int edge in new[]{1,2,4})
                {
                    int j=i^edge;
                    if(i<j&&depth[i]>Near!=depth[j]>Near)Take(Vector3.Lerp(corners[i],corners[j],(Near-depth[i])/(depth[j]-depth[i])));
                }
            }
            return Mathf.Max(0,Mathf.Min(xMax,1)-Mathf.Max(xMin,0))*Mathf.Max(0,Mathf.Min(yMax,1)-Mathf.Max(yMin,0));
        }
        static IEnumerable<Vector3> Paint(TextMeshPro sign)
        {
            var size=sign.GetPreferredValues(sign.text);float w=size.x+.3f,h=size.y;
            for(int i=0;i<=8;i++)for(int j=0;j<=2;j++)yield return sign.transform.TransformPoint(new Vector3((i/8f-.5f)*w,(j/2f-.5f)*h,0));
        }

        // ---------------------------------------------------------------- item 1: the first job
        [UnityTest] public IEnumerator TheFirstJobsKitStaysOutWithOnlyItsEdgeGone()
        {
            GameServices.Current.NewGame();Flag(OpeningMarketSequence.Requested);
            yield return Zone("Hub_Decks","Office");yield return null;yield return null;
            var kit=All<Transform>().First(t=>t.name=="Service job kit");
            Transform[] Left()=>kit.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="DressCableDrum"||t.name=="DressWorkLamp"||t.name=="DressPowerCell").Where(t=>t.GetComponentInChildren<Renderer>(true)!=null&&t.parent.name=="Service job pieces").ToArray();
            var pieces=Left();Assert.GreaterOrEqual(pieces.Length,3,"fixture: the drum, the terminal and the cell");
            var spots=pieces.Select(t=>t.position).ToArray();
            var edges=All<Transform>().Where(t=>t.name=="Opening edge").ToArray();Assert.GreaterOrEqual(edges.Length,4,"fixture: the hole's painted edge");
            Assert.IsTrue(edges.All(e=>e.gameObject.activeInHierarchy),"fixture: the job's on");
            Flag(OpeningMarketSequence.Repaired);yield return null;yield return null;
            Assert.IsFalse(edges.Any(e=>e.gameObject.activeInHierarchy),"the painted edge stays round a hole that's been closed");
            for(int i=0;i<pieces.Length;i++)
            {
                Assert.IsTrue(pieces[i].gameObject.activeInHierarchy,pieces[i].name+" vanishes the moment the repair ends, with nobody carrying it off: the dressing looks switched off, not finished with");
                Assert.Less(Vector3.Distance(pieces[i].position,spots[i]),.01f,pieces[i].name+" moves when nobody's there to move it");
            }
            Flag("warpkey");yield return null;yield return null;
            Assert.IsFalse(kit.gameObject.activeInHierarchy,"the walkway job's kit is still out when the party's back from Sorrel");
        }

        // ---------------------------------------------------------------- item 2: a sign's plate on bright ground
        static float FeatherOf(Transform plate)
        {
            var mesh=plate.GetComponent<MeshFilter>().sharedMesh;var at=mesh.vertices;var tone=mesh.colors32;float solid=0,clear=0;
            for(int i=0;i<at.Length;i++){if(tone[i].a==255)solid=Mathf.Max(solid,Mathf.Abs(at[i].x));else clear=Mathf.Max(clear,Mathf.Abs(at[i].x));}
            return clear-solid;
        }
        [UnityTest] public IEnumerator ASignsPlateIsASoftShadeOnBrightGroundAndStaysOnTheDecks()
        {
            yield return Zone("Sorrel_Ridges");
            var bright=All<TextMeshPro>().First(t=>t.text=="SORREL OUTPOST").transform.Find("Sign plate");Assert.IsNotNull(bright,"fixture: the sign's plate");
            Assert.Less(bright.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor").a,.5f,"on Sorrel's tan the plate is a dirty brown rectangle with tyre tracks running through it");
            Assert.Greater(FeatherOf(bright),.7f,"the plate on bright ground stops within a hand of its words: it still has edges");
            // Its lower rim comes down to the floor at nothing. Feathered on past that, the floor cuts the paint off in a hard straight line, whatever its other three sides do.
            Assert.IsTrue(Physics.Raycast(bright.parent.position+Vector3.up*.3f,Vector3.down,out var ground,8,~0,QueryTriggerInteraction.Ignore),"fixture: ground under the sign");
            var lowest=float.PositiveInfinity;foreach(var at in bright.GetComponent<MeshFilter>().sharedMesh.vertices)lowest=Mathf.Min(lowest,bright.TransformPoint(at).y);
            Assert.Greater(lowest,ground.point.y-.1f,"the plate's lower feather runs under the floor, which slices it in a hard straight edge: a rectangle's side");
            yield return Zone("Hub_Decks");
            var dark=All<TextMeshPro>().First(t=>t.name.StartsWith("Sign_")&&t.transform.Find("Sign plate")!=null&&t.text.Contains("RECEIVING")).transform.Find("Sign plate");
            Assert.Greater(dark.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor").a,.55f,"the Decks' dark floors lost the plate that does no harm there");
            Assert.Less(FeatherOf(dark),.3f,"the Decks' plate has spread out for nothing");
        }

        // ---------------------------------------------------------------- item 3: paint, and a rig that's been wrecked
        [UnityTest] public IEnumerator TheAnvilAndTheRigAreNotPaintedWhite()
        {
            yield return Zone("Sorrel_Ridges");
            foreach(var name in new[]{"LatticeAnvil","OutpostDrill"})
            {
                var look=All<Transform>().First(t=>t.name==name&&t.gameObject.activeInHierarchy).GetComponentInChildren<Renderer>().sharedMaterial;
                var bright=new List<float>();foreach(var pixel in Read(look.GetTexture("_BaseMap")))bright.Add(pixel.grayscale);
                bright.Sort();
                Assert.Less(bright[(int)(bright.Count*.95f)],.72f,name+": its cream parts are near white paint, the brightest thing on the screen beside hand-painted steel");
            }
        }
        [UnityTest] public IEnumerator TheDrillSitesRigLooksWrecked()
        {
            yield return Zone("Sorrel_Ridges");
            var rig=All<Transform>().First(t=>t.name=="OutpostDrill"&&t.parent==null);
            Assert.IsNotNull(rig.GetComponent<PropTint>(),"the rig stands clean beside the hole a boss came out of");
            Assert.IsNotNull(rig.GetComponent<StaysDark>(),"the wrecked rig's indicators are still running");
            Assert.Greater(Quaternion.Angle(rig.GetChild(0).localRotation,Quaternion.identity),3,"the rig stands square on its footing with its arm upright");
            var loose=All<Transform>().FirstOrDefault(t=>t.name=="Rig wreckage");Assert.IsNotNull(loose,"nothing shook loose");
            Assert.GreaterOrEqual(loose.childCount,4,"nothing shook loose");
            Assert.IsEmpty(loose.GetComponentsInChildren<Collider>(),"what shook loose is solid and off the bake");
            foreach(var part in loose.GetComponentsInChildren<Renderer>())Assert.Less(part.bounds.max.y,6,part.name+" lies too high to be lying about");
        }

        // ---------------------------------------------------------------- item 4: strata keep to far slopes
        static readonly Vector2[][] SorrelRoads=
        {
            new Vector2[]{new(0,-18),new(0,8),new(0,24),new(9,39),new(4,58),new(-8,75),new(-5,94),new(9,111),new(16,129),new(7,145),new(0,159),new(-5,177),new(0,199)},
            new Vector2[]{new(-5,27),new(-24,35),new(-36,49),new(-39,68),new(-28,88),new(-24,107),new(-13,124),new(7,145)},
            new Vector2[]{new(9,39),new(29,46),new(39,64),new(31,83),new(35,103),new(31,123),new(16,129)},
        };
        /// <summary>The readers' orange brick stacks are this piece, at (2.6, 207.2) above the key's cradle and (12.7, 56.2) on the haul road. QaWorldSecondTests' kerb check reads the other strata piece.</summary>
        [UnityTest] public IEnumerator NoStackOfOrangeBricksStandsByTheRoads()
        {
            yield return Zone("Sorrel_Ridges");
            var stacks=new List<string>();
            foreach(var piece in All<Transform>().Where(t=>t.name=="DressStrataOutcrop"&&t.parent!=null&&t.parent.name=="Room dressing"))
            {
                var at=new Vector2(piece.position.x,piece.position.z);float near=float.PositiveInfinity;
                foreach(var road in SorrelRoads)for(int i=1;i<road.Length;i++)near=Mathf.Min(near,ToSegment(at,road[i-1],road[i]));
                if(near<14)stacks.Add($"({at.x:0.0}, {at.y:0.0})");
            }
            Assert.IsEmpty(stacks,"a stack of bright orange mortar-lined blocks stands in the open, a brick planter on a wild ridge, at "+string.Join(", ",stacks));
        }

        // ---------------------------------------------------------------- item 5: the later cradles
        static void LooksLikeACradle(RepairBay bay,string name)
        {
            var around=All<Transform>().FirstOrDefault(t=>t.name==name+" bay");
            Assert.IsNotNull(around,name+" is a shut box that stands around the deck as storage: nothing says this is where you heal");
            Assert.IsEmpty(around.GetComponentsInChildren<Collider>(),name+"'s bay is solid and off the bake");
            Assert.IsNotEmpty(around.GetComponentsInChildren<LineRenderer>(),name+" has no column of light to find it by");
            Assert.GreaterOrEqual(around.GetComponentsInChildren<Transform>().Count(t=>t.name=="Bay bollard"),2,name+" has no lit posts");
            Assert.Greater(bay.GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_EmissionColor").maxColorComponent,1.5f,name+": nothing on it is lit");
        }
        [UnityTest] public IEnumerator TheLaterCradlesLookLikeOne()
        {
            yield return Zone("TallowDrift");
            LooksLikeACradle(All<RepairBay>().Single(),"Refuge cradle");
            yield return Zone("SunkenRelay");
            var bay=All<RepairBay>().Single();LooksLikeACradle(bay,"Relay cradle");
            Assert.IsEmpty(All<Transform>().First(t=>t.name=="Relay cradle bay").GetComponentsInChildren<Light>(),"this floor is one renderer, well past its six lights: a seventh puts another pool out somewhere in the hall");
            // The route stops here, and the post stood on the spot in a first draft.
            foreach(var post in All<Transform>().Where(t=>t.name=="Bay bollard"))Assert.Greater(Flat(post.position,new Vector3(6.8f,0,108.8f)),1.2f,"a lit post stands where the party stops at the cradle");
            var sign=All<TextMeshPro>().Single(t=>t.text=="RELAY CRADLE");
            foreach(var thing in All<Transform>().Where(t=>(t.name=="HushwellCrates"||t.name=="LanternShard")&&t.parent==null))
                Assert.Greater(Flat(thing.position,new Vector3(8,0,104)),5,$"a {thing.name} lies just under RELAY CRADLE on the screen: a dark crate and a rusty cage read as the cradle, with the real one under the status panel");
            Assert.Less(Flat(sign.transform.position,bay.transform.position),6,"fixture: the cradle's sign");
        }

        // ---------------------------------------------------------------- item 6: Tallow's lift sign and galley picture
        [UnityTest] public IEnumerator TallowsLiftSignIsOnItsOwnDoor()
        {
            yield return Zone("TallowDrift");yield return null;
            var sign=All<TextMeshPro>().Single(t=>t.text=="PRESSURE LIFT");var launch=All<DockingPad>().Single(d=>d.scene=="TallowApproach");
            Assert.Less(Mathf.Abs(sign.transform.position.x),.5f,"fixture: the lift's door is at x 0");
            Assert.Less(Mathf.Abs(sign.transform.position.z+7),.6f,"PRESSURE LIFT lies off the door it names");
            Put(Lead,new Vector3(launch.transform.position.x,.05f,launch.transform.position.z));yield return new WaitForSecondsRealtime(1.2f);
            var camera=Camera.main;var eye=camera.transform.position;
            foreach(var point in Paint(sign))
            {
                Assert.IsTrue(Inside(camera.WorldToViewportPoint(point)),"from the launch spot PRESSURE LIFT runs off the screen");
                foreach(var hit in Physics.RaycastAll(eye,(point-eye).normalized,Vector3.Distance(eye,point)-.05f,~0,QueryTriggerInteraction.Ignore))
                    Assert.IsFalse(hit.collider.name.StartsWith("Door"),$"from the launch spot {hit.collider.name} stands across PRESSURE LIFT: the reader's 'RESS'");
            }
            Put(Lead,new Vector3(0,.05f,1.6f));yield return new WaitForSecondsRealtime(1.2f);
            bool promptUp=All<UnityEngine.UI.Image>().Any(i=>i.name=="InteractionPlate"&&i.isActiveAndEnabled);
            foreach(var point in Paint(sign))
            {
                var view=Camera.main.WorldToViewportPoint(point);
                // On a lintel over the walkway it stays where it is, so with a prompt up it steps aside.
                Assert.IsFalse(promptUp&&sign.alpha>.1f&&view.y>.12f&&view.y<.4f&&view.x>.3f&&view.x<.7f,"from Abri's stop PRESSURE LIFT stands over the prompt, like a heading for 'Talk to Keeper Abri'");
            }
            // Only a lintel is overhead. The Decks' floor tiles have another deck 3 m under them, and a hero still stands on what's painted there.
            yield return Zone("Hub_Decks");
            foreach(var words in new[]{"MARKET / GALLEY","RECEIVING / REPAIR","CABINS"})
                Assert.IsFalse(All<TextMeshPro>().First(t=>t.text.StartsWith(words)).GetComponent<SignFade>().bossOnly,$"{words} lies on a deck with a deck under it, and no longer thins out under the hero standing on it");
        }
        [UnityTest] public IEnumerator TallowsGalleyPictureIsNeverCutByAPlate()
        {
            yield return Zone("TallowDrift");
            var box=All<MeshRenderer>().First(r=>r.name=="Floor detail PictoGalley").bounds;
            Put(Lead,new Vector3(0,.05f,1.6f));yield return new WaitForSecondsRealtime(1.2f);
            // The dialogue box runs from 40 to 370 up from the foot of 1080: all of the picture is in that band, so it's whole with no box and whole under one. The hint bar stops at .075.
            foreach(var (x,z) in new[]{(box.min.x,box.min.z),(box.max.x,box.min.z),(box.min.x,box.max.z),(box.max.x,box.max.z)})
            {
                var view=Camera.main.WorldToViewportPoint(new Vector3(x,box.center.y,z));
                Assert.Greater(view.y,.08f,"from Abri's stop the button bar slices the bowl, and a stray squiggle pokes out under her dialogue box");
                Assert.Less(view.y,.34f,"the bowl pokes up over the top of her dialogue box");
                Assert.Greater(view.x,.045f,"the bowl runs off the side of the box");
            }
        }

        // ---------------------------------------------------------------- item 7: scenery that's plainly scenery
        [UnityTest] public IEnumerator TheReachsWreckageHasNoRaiderCrates()
        {
            yield return Zone("WickReach");
            Assert.IsEmpty(All<Transform>().Where(t=>t.name=="Wreckage MusterCrate"),"black crates with red corners drift through the fights, dark on dark and in the raiders' own red: is that a hazard or a target?");
            Assert.Greater(All<Transform>().Count(t=>t.name.StartsWith("Wreckage ")),10,"fixture: the Reach still has its wreckage");
        }

        // ---------------------------------------------------------------- item 8: the curtain
        [UnityTest] public IEnumerator TheCurtainTakesItsTimeWhereTheViewIs()
        {
            yield return Zone("WickReach");
            var strands=All<LineRenderer>().Where(l=>l.name=="Curtain strand").ToArray();var full=strands.ToDictionary(s=>s,s=>s.startColor.a);
            // The view shows the stretch nearest the ship, about 13 m either side.
            var near=strands.Where(s=>Mathf.Abs(s.transform.TransformPoint(s.GetPosition(0)).x)<13).ToArray();Assert.Greater(near.Length,5,"fixture: the curtain's middle");
            Flag("wick.relay1");Flag("wick.relay2");
            float deadline=Time.unscaledTime+3;while(!Has("wick.gateOpen")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("wick.gateOpen"),"fixture: two relays open the gate");
            yield return new WaitForSecondsRealtime(2);
            Assert.Greater(near.Count(s=>s.gameObject.activeInHierarchy&&s.startColor.a/full[s]>.12f),near.Length/2,"two seconds on, the stretch in view has gone while Sela's line about it is still up: the player can miss the payoff of lighting both relays");
        }

        // ---------------------------------------------------------------- item 9: the first look at the town
        [UnityTest] public IEnumerator TheFirstLookIsOfASquareNotOfAnArchsBack()
        {
            yield return Zone("Lanternwick");yield return new WaitForSecondsRealtime(.8f);
            var arch=All<SceneryOcclusion>().Single(s=>s.name=="Market arch");var box=BoundsOf(arch);var camera=Camera.main;var eye=camera.transform.position;
            foreach(var member in PartyController.Current.members)foreach(float side in new[]{-1.5f,0,1.5f})foreach(float up in new[]{.4f,1,1.8f})
            {
                var target=member.transform.position+camera.transform.right*side+Vector3.up*up;
                Assert.IsFalse(box.IntersectRay(new Ray(eye,target-eye),out float along)&&along<Vector3.Distance(eye,target),"the arch stands between the camera and the arriving party: the first look at the town is its back, the party small between its piers");
            }
            var disc=All<Transform>().First(t=>t.name=="HushwellMarker"&&t.parent==null);
            var pad=new Bounds(disc.position,Vector3.zero);foreach(var part in disc.GetComponentsInChildren<Renderer>())pad.Encapsulate(part.bounds);
            foreach(var (x,z) in new[]{(pad.min.x,pad.center.z),(pad.max.x,pad.center.z),(pad.center.x,pad.max.z)})
            {
                var corner=new Vector3(x,pad.center.y,z);
                Assert.IsTrue(Inside(camera.WorldToViewportPoint(corner)),"the square's carved pad runs off the first screen");
                Assert.IsFalse(box.IntersectRay(new Ray(eye,corner-eye),out float along)&&along<Vector3.Distance(eye,corner),"a brown disc lies half hidden behind the arch on the first look at the town");
            }
            Assert.IsNotNull(disc.GetComponent<StaysDark>(),"cyan arcs burn in a carved pad in a town of amber");
        }

        // ---------------------------------------------------------------- item 10: the gorge
        [UnityTest] public IEnumerator TheCampsTentStandsClearOfRock()
        {
            yield return Zone("LanternGorge");
            var tent=BoundsOf(All<Transform>().First(t=>t.name=="MusterLeanTo"&&t.parent==null));
            foreach(var rock in All<Transform>().Where(t=>t.parent==null&&(t.name=="DressBoulders"||t.name.StartsWith("RidgeRock")||t.name.StartsWith("DressStrata"))))
                Assert.IsFalse(tent.Intersects(BoundsOf(rock)),$"{rock.name} at ({rock.position.x:0.0},{rock.position.z:0.0}) stands through the scavengers' tent, with a peg poking out of the stone");
        }
        [UnityTest] public IEnumerator TheDenLooksLikeADenUnderATower()
        {
            yield return Zone("LanternGorge");
            var lamp=All<DiscoveryPoint>().Single(p=>p.flag=="pell.lampFound").transform.position;
            var tower=All<Transform>().Where(t=>t.name=="Fallen relay tower").OrderBy(t=>Flat(t.position,lamp)).First();var box=BoundsOf(tower);
            var nearest=new Vector3(Mathf.Clamp(lamp.x,box.min.x,box.max.x),lamp.y,Mathf.Clamp(lamp.z,box.min.z,box.max.z));
            Assert.Less(Vector3.Distance(nearest,lamp),6.5f,"the quest says the den is under the fallen tower, and from the den's floor the tower is off the top of the screen");
            ClearAll();
            Put(Lead,new Vector3(-24.5f,lamp.y+.3f,99.5f));yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(Inside(Camera.main.WorldToViewportPoint(new Vector3(-24.5f,box.min.y+.5f,box.min.z))),"the fallen tower isn't in view where you take the lamp");
            var den=All<Transform>().FirstOrDefault(t=>t.name=="Grazer den");Assert.IsNotNull(den,"the den is open floor with an agave on it");
            Assert.GreaterOrEqual(den.childCount,6,"the den is open floor with an agave on it");
            Assert.IsEmpty(den.GetComponentsInChildren<Collider>(),"the den's clutter is solid and off the bake");
            Assert.IsNotNull(den.Find("Grazer den dirt"),"nothing says this ground is lived on");
        }
        static void ClearAll(){foreach(var fight in All<EncounterVolume>())Flag("clear."+fight.encounterId);}
        [UnityTest] public IEnumerator TheGorgesTwoEndsHaveSomethingOnThem()
        {
            yield return Zone("LanternGorge");
            var marks=All<Transform>().Where(t=>t.name=="Road lantern").Select(t=>t.position).ToArray();
            Assert.GreaterOrEqual(marks.Count(p=>p.z<20),2,"the arrival is flat mauve floor with one small prop on the screen");
            Assert.GreaterOrEqual(marks.Count(p=>p.z>210),3,"the last stretch before the relay door is flat mauve floor");
            foreach(var mark in All<Transform>().Where(t=>t.name=="Road lantern"))Assert.IsEmpty(mark.GetComponentsInChildren<Collider>(),"a road lantern is solid and off the bake");
            var floor=All<MeshFilter>().Single(f=>f.name.EndsWith(" floor")).sharedMesh;var points=floor.vertices;var tones=floor.colors;
            Color Tone(float x,float z){int best=0;float near=float.MaxValue;for(int i=0;i<points.Length;i++){float d=(points[i].x-x)*(points[i].x-x)+(points[i].z-z)*(points[i].z-z);if(d<near){near=d;best=i;}}return tones[best];}
            Assert.Greater(Tone(1,100).grayscale-Tone(-9.5f,88).grayscale,.06f,"nothing on the floor leads the eye: one flat colour from wall to wall");
        }
        /// <summary>Where a floor's edge turns back on itself. HEAD's gorge has about 22% of its edge in teeth, and an offline model with the diagonal along the contour about 7%.</summary>
        static float ToothShare(Mesh floor)
        {
            var at=floor.vertices;var tri=floor.triangles;var uses=new Dictionary<long,int>();
            void Use(int a,int b){long key=System.Math.Min(a,b)*(long)at.Length+System.Math.Max(a,b);uses.TryGetValue(key,out int n);uses[key]=n+1;}
            for(int t=0;t<tri.Length;t+=3){Use(tri[t],tri[t+1]);Use(tri[t+1],tri[t+2]);Use(tri[t+2],tri[t]);}
            var links=new Dictionary<int,List<int>>();
            foreach(var (key,n) in uses)
            {
                if(n!=1)continue;
                int a=(int)(key/at.Length),b=(int)(key%at.Length);
                if(!links.TryGetValue(a,out var la))links[a]=la=new List<int>();la.Add(b);
                if(!links.TryGetValue(b,out var lb))links[b]=lb=new List<int>();lb.Add(a);
            }
            int points=0,teeth=0;
            foreach(var (v,near) in links)
            {
                if(near.Count!=2)continue;
                var one=at[near[0]]-at[v];var two=at[near[1]]-at[v];one.y=two.y=0;points++;
                if(Vector3.Dot(one.normalized,two.normalized)>-.17f)teeth++;
            }
            return teeth/(float)Mathf.Max(1,points);
        }
        [UnityTest] public IEnumerator TheGorgesFloorMeetsItsWallsInALine()
        {
            yield return Zone("LanternGorge");
            float share=ToothShare(All<MeshFilter>().Single(f=>f.name.EndsWith(" floor")).sharedMesh);
            Assert.Less(share,.13f,$"{share*100:0}% of the points along the floor's edge are teeth: hard triangles of rock and floor along the cliff foot");
        }
        /// <summary>The reader's row of dashes at Gorge_0, read off s14/017: static across five frames, pale, inside a shadow, and not in the floor's texture. Tracing back along the sun, its points reach a far ridge's lit slope 46 m away.</summary>
        [UnityTest] public IEnumerator WallsCastShadowFromBothSides()
        {
            yield return Zone("LanternGorge");
            var cliffs=All<MeshFilter>().Single(f=>f.name.EndsWith(" cliffs")).GetComponent<MeshRenderer>();
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.TwoSided,cliffs.shadowCastingMode,"the walls' unshared triangles open hairline cracks under the shadow pass's normal bias: a straight row of pale dashes across a shaded floor");
        }

        // ---------------------------------------------------------------- item 11: the town
        [UnityTest] public IEnumerator TheLanternStairIsAStair()
        {
            yield return Zone("Lanternwick");
            var stair=All<MeshRenderer>().FirstOrDefault(r=>r.name=="Lantern stair");
            Assert.IsNotNull(stair,"the objective sends you to the lantern stair, and there's a long ribbed strip beside a block stack, with the keeper on flat floor");
            var mesh=stair.GetComponent<MeshFilter>().sharedMesh;var at=mesh.vertices;var heights=new HashSet<int>();foreach(int i in mesh.GetTriangles(0))heights.Add(Mathf.RoundToInt(at[i].y*100));
            Assert.GreaterOrEqual(heights.Count,8,"it has no steps");
            Assert.Greater(stair.bounds.size.y,2.5f,"it goes nowhere");
            Assert.GreaterOrEqual(stair.GetComponentsInChildren<Transform>().Count(t=>t.name=="LampPost"),4,"nothing lit stands on the lantern stair");
            var aumi=All<Npc>().Single(n=>n.speaker=="Aumi");
            Assert.Less(Flat(aumi.transform.position,stair.transform.position),4,"the keeper doesn't stand at its foot");
            Assert.IsEmpty(stair.GetComponents<Collider>(),"the camera takes a rendered collider for scenery to dive past");
            Assert.IsNotEmpty(stair.GetComponentsInChildren<BoxCollider>(),"anyone can walk through the steps");
            // Where the route stops to talk to her, its foot and its first steps are on the screen and in front of no one.
            var stop=new Vector3(20,3.2f,53.5f);Put(Lead,stop);yield return new WaitForSecondsRealtime(1.2f);
            var camera=Camera.main;
            foreach(var (along,up) in new[]{(0f,0f),(2.5f,1f)})Assert.IsTrue(Inside(camera.WorldToViewportPoint(stair.transform.TransformPoint(new Vector3(0,up,along)))),"the stair's foot isn't in view where you talk to her");
            var bounds=new Bounds(stop+Vector3.up*1,new Vector3(.8f,1.8f,.8f));
            foreach(var block in stair.GetComponentsInChildren<BoxCollider>())Assert.IsFalse(block.bounds.Intersects(bounds),"the stair stands on the route's stop");
            var lamp=All<Transform>().Single(t=>t.name=="Pell's lamp").position;
            var local=stair.transform.InverseTransformPoint(lamp);
            Assert.Less(Mathf.Abs(local.x),1.8f,"'It's on the stair, where she kept it', and it isn't");Assert.Greater(local.z,0,"'It's on the stair, where she kept it', and it isn't");
        }
        [UnityTest] public IEnumerator NoHomeFillsTheScreenOnTheWayToTheOrchard()
        {
            yield return Zone("Lanternwick");
            Put(Lead,new Vector3(-15.2f,.2f,74.5f));yield return new WaitForSecondsRealtime(1.5f);
            foreach(var home in All<Transform>().Where(t=>t.name=="CanyonHouse"&&t.parent==null))
            {
                // By its box, which is more than its shape. Model: the home that filled this frame 49%, 17% where it stands now.
                float share=ScreenShare(BoundsOf(home));
                Assert.Less(share,.2f,$"the home at ({home.position.x:0},{home.position.z:0}) fills {share*100:0}% of the screen on the way to the orchard: a stack of blocks with loose planks on top");
            }
            // Where the route stands through Aumi's whole chart talk. Model: 19% at (11, 52), 6% at (11, 68), where what's left of it is under the status panel.
            Put(Lead,new Vector3(19.78f,3.2f,52.88f));yield return new WaitForSecondsRealtime(1.5f);
            foreach(var home in All<Transform>().Where(t=>t.name=="CanyonHouse"&&t.parent==null&&t.position.x<16&&t.position.x>0))
                Assert.Less(ScreenShare(BoundsOf(home)),.08f,"a brick pile fills the corner of the shot through Aumi's big talk");
            var brekk=All<Npc>().Single(n=>n.speaker=="Brekk").transform.position;
            var shed=All<Transform>().Where(t=>t.name=="CanyonHouse"&&t.parent==null).OrderBy(t=>Flat(t.position,brekk)).First();
            Assert.Less(BoundsOf(shed).max.x,-33.5f,"the shed's back fills the foot of the screen on the way on to the orchard gate");
        }

        // ---------------------------------------------------------------- item 12: the relay
        /// <summary>What LatticeToon adds for a lamp: .6 of its strength, scaled by a contribution that saturates at 1. Under one of these lamps the floor gets about .43.</summary>
        [UnityTest] public IEnumerator TheRelaysLampsDoNotBurnItsWalls()
        {
            yield return Zone("SunkenRelay");
            var walls=All<MeshFilter>().Single(f=>f.name.EndsWith(" cliffs")).sharedMesh;var at=walls.vertices;var normal=walls.normals;
            var lamps=All<Light>().Where(l=>l.name=="Lamp light").ToArray();Assert.Greater(lamps.Length,8,"fixture: the relay's lamps");
            foreach(var lamp in lamps)
            {
                var from=lamp.transform.position;float worst=0;
                for(int i=0;i<at.Length;i++)
                {
                    var to=from-at[i];float d2=to.sqrMagnitude;if(d2>49)continue;
                    float window=Mathf.Clamp01(1-(d2/(lamp.range*lamp.range))*(d2/(lamp.range*lamp.range)));
                    worst=Mathf.Max(worst,.6f*lamp.intensity*Mathf.Clamp01(Mathf.Clamp01(Vector3.Dot(normal[i],to.normalized))*window*window/Mathf.Max(d2,.0001f)));
                }
                Assert.Less(worst,1,$"the lamp at ({from.x:0.0},{from.z:0.0}) lights the wall beside it to {worst:0.0}, past twice what the floor under it gets: hot stripes up the strata with no lamp in view, a sunken hall that looks sunlit");
            }
        }

        // ---------------------------------------------------------------- item 13: the nursery
        [UnityTest] public IEnumerator NobodyWalksOnTheNurserysWater()
        {
            yield return Zone("Hushwell");
            var edges=All<NavMeshObstacle>().Where(o=>o.name=="Pool edge").ToArray();
            Assert.AreEqual(3,edges.Length,"the pools read as water now they have a shore, and the partner stands out on them as if they were floor");
            yield return new WaitForSecondsRealtime(1);
            foreach(var edge in edges)
            {
                Assert.IsTrue(edge.carving,"the partner's path still runs across a pool");
                Assert.IsFalse(NavMesh.SamplePosition(edge.transform.position,out _,.8f,NavMesh.AllAreas),$"the pool at ({edge.transform.position.x:0},{edge.transform.position.z:0}) is still walkable floor to the partner");
                var solid=edge.GetComponent<MeshCollider>();Assert.IsNotNull(solid,"the hero wades through a pool with no ripple");Assert.IsTrue(solid.convex,"fixture: a kerb is a solid prism");
                Assert.IsNull(edge.GetComponent<Renderer>(),"the kerb is drawn");Assert.IsFalse(edge.gameObject.isStatic,"the bake reads the kerb and leaves a walkable island on top of it");
            }
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(25.2f,HushwellLayout.Lower,340.2f),out _,1.5f,NavMesh.AllAreas),"the way to the lift has gone with the pools");
        }
        /// <summary>Against the good cradle's own eggs, read the same way, so this says the same in any colour space.</summary>
        static float EggPaint(string kit)
        {
            // Lit only: the cradles room dressing retired keep a body of this name, and asked of an object that's switched off GetComponentInChildren says nothing.
            var look=All<Transform>().First(t=>t.name==kit&&t.gameObject.activeInHierarchy).GetComponentInChildren<Renderer>().sharedMaterial;var bright=new List<float>();
            foreach(var pixel in Read(look.GetTexture("_BaseMap")))if(pixel.b>pixel.r*1.3f&&pixel.b>pixel.g*1.3f&&pixel.g<.6f)bright.Add(pixel.grayscale);
            Assert.Greater(bright.Count,200,"fixture: "+kit+" has its eggs");
            return Median(bright);
        }
        [UnityTest] public IEnumerator EveryCradlesEggsArePaintedAsTheGoodOnes()
        {
            yield return Zone("Hushwell");
            float good=EggPaint("HushwellEggCradle");
            foreach(var kit in new[]{"HushwellCradleSingle","HushwellCradleHatched"})
            {
                Assert.Greater(EggPaint(kit),good*.85f,kit+" holds plain navy lumps, flat and crumpled, next to the good glossy eggs");
                var look=All<Transform>().First(t=>t.name==kit&&t.gameObject.activeInHierarchy).GetComponentInChildren<Renderer>().sharedMaterial;
                Assert.Greater(look.GetColor("_RimColor").maxColorComponent,.5f,kit+": its eggs have no shine");
                Assert.Greater(look.GetFloat("_RimUnlit"),.3f,kit+": a dim cave's light never reaches the shine on its eggs");
            }
        }

        // ---------------------------------------------------------------- item 14: Tallow Approach
        [UnityTest] public IEnumerator TheDocksRingIsOneBand()
        {
            yield return Zone("TallowApproach");
#if UNITY_EDITOR
            var dock=All<DockingPad>().Single().transform.position;var row=new List<(float angle,float y)>();
            foreach(var mesh in All<MeshFilter>().Where(f=>f.name.Contains("DecalHazard")).Select(f=>f.sharedMesh))
                foreach(var at in mesh.vertices)
                {
                    float reach=Mathf.Sqrt((at.x-dock.x)*(at.x-dock.x)+(at.z-dock.z)*(at.z-dock.z));
                    if(reach>5.2f&&reach<6.1f)row.Add((Mathf.Atan2(at.z-dock.z,at.x-dock.x)*Mathf.Rad2Deg,at.y));
                }
            Assert.Greater(row.Count,48,"fixture: the ring's points");
            for(int bin=-180;bin<180;bin+=15)Assert.IsTrue(row.Any(p=>p.angle>=bin&&p.angle<bin+15),$"the dock's ring has a gap at {bin} degrees");
            row.Sort((a,b)=>a.angle.CompareTo(b.angle));
            for(int i=1;i<row.Count;i++)Assert.Less(Mathf.Abs(row[i].y-row[i-1].y),.8f,$"the ring falls down a hole in the hull at {row[i].angle:0} degrees");
#endif
        }
        [UnityTest] public IEnumerator TheDocksLegsAreSeated()
        {
            yield return Zone("TallowApproach");
            var struts=All<Transform>().Where(t=>t.name=="DeckWall"&&t.parent==null&&Mathf.Abs(Mathf.Abs(t.position.x)-2)<.01f&&Mathf.Abs(t.position.z-.5f)<.01f).ToArray();Assert.AreEqual(2,struts.Length,"fixture: the apron's two struts");
            foreach(var strut in struts)Assert.Less(strut.GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_BaseColor").grayscale,.5f,"the apron stands on pale flat sticks");
            var feet=All<Transform>().FirstOrDefault(t=>t.name=="Dock strut feet");Assert.IsNotNull(feet,"the apron's legs stop short on the hull with no contact shadow");
            Assert.AreEqual(2,feet.childCount,"fixture: a foot for each");
            foreach(var part in feet.GetComponentsInChildren<Renderer>())Assert.Less(part.bounds.max.y,-1,"a strut's foot isn't lying on the hull");
            Assert.IsEmpty(All<MeshCollider>().Where(c=>c.name=="Dock survey"),"the survey collider stays in the scene: a hull a ship would hit");
        }

        // ---------------------------------------------------------------- item 15: a door opens only for a real line
        /// <summary>The reader's frame: hero at (-29.4, 59.7) beside a fruit tree's crown, with his chest clear of it and the crown's box round the line.</summary>
        [UnityTest] public IEnumerator AFruitTreeOpensOnlyForARealLineToTheChest()
        {
            yield return Zone("Lanternwick");
            var tree=All<SceneryOcclusion>().Where(s=>s.name=="OrchardTree").OrderBy(s=>Flat(s.transform.position,new Vector3(-31,0,56))).First();
            Assert.Less(Flat(tree.transform.position,new Vector3(-31.1f,0,56)),1,"fixture: the reader's tree");
            Put(Lead,new Vector3(-29.39f,1.6f,59.66f));yield return new WaitForSecondsRealtime(1.2f);
            var box=BoundsOf(tree);var eye=Camera.main.transform.position;var chest=Lead.transform.position+Vector3.up*.95f;
            Assert.IsTrue(box.IntersectRay(new Ray(eye,chest-eye),out float along)&&along<Vector3.Distance(eye,chest),"fixture: the line to the chest crosses the tree's box");
            Assert.Less(Faded(tree),.05f,"a cloud of dark dots is eaten out of a fruit tree that isn't hiding anyone");
            // And a real one still opens. Behind a home, with the hero's chest well within its walls from the camera.
            var home=All<SceneryOcclusion>().Where(s=>s.name=="CanyonHouse").OrderBy(s=>Flat(s.transform.position,new Vector3(-12,0,40))).First();var walls=BoundsOf(home);
            Put(Lead,new Vector3(walls.center.x,.2f,walls.max.z+1));yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(Faded(home),.5f,"a hero behind a home is hidden, with no hole opened for him");
        }
    }
}
