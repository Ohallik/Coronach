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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The fourth eye pass on the places, the last before the build goes out. A sign left behind when its stair was built, two wrenches a step apart, a door lit like a fire behind the party, a stain with nothing to cast it, a ring that reads as a debug circle.
    /// These pin what readers saw to its cause. Whether it now looks finished is for eyes.</summary>
    public sealed class QaLastPlacesTests
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
        static IEnumerable<Vector3> Paint(TextMeshPro sign)
        {
            var size=sign.GetPreferredValues(sign.text);float w=size.x+.3f,h=size.y;
            for(int i=0;i<=8;i++)for(int j=0;j<=2;j++)yield return sign.transform.TransformPoint(new Vector3((i/8f-.5f)*w,(j/2f-.5f)*h,0));
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

        // ---------------------------------------------------------------- item 7: two signs by what they name
        [UnityTest] public IEnumerator TheStairsSignLiesAtTheStairsFoot()
        {
            yield return Zone("Lanternwick");
            var sign=All<TextMeshPro>().Single(t=>t.text=="LANTERN STAIR");var stair=All<MeshRenderer>().First(r=>r.name=="Lantern stair").transform;
            Assert.Less(Flat(sign.transform.position,stair.position),4,"LANTERN STAIR lies on open floor by a lamp post, well short of the stair: it names bare ground, and the stair carries only the keeper's name");
            Assert.Less(stair.InverseTransformPoint(sign.transform.position).z,0,"LANTERN STAIR lies on the steps or behind them, not on the floor at their foot");
            // Where the route stops to talk to her.
            Put(Lead,new Vector3(20,3.2f,53.5f));yield return new WaitForSecondsRealtime(1.2f);
            foreach(var point in Paint(sign))Assert.IsTrue(Inside(Camera.main.WorldToViewportPoint(point)),"where the party stops to talk to the keeper, the stair's sign is off the screen");
        }
        [UnityTest] public IEnumerator TheCourierSignStandsAgainstTheMat()
        {
            yield return Zone("Lanternwick");
            var sign=All<TextMeshPro>().Single(t=>t.text=="COURIER DOCK");var mat=BoundsOf(All<Transform>().First(t=>t.name=="HushwellMarker"&&t.parent==null));
            float gap=(sign.transform.position-sign.transform.right*sign.GetPreferredValues(sign.text).x*.5f).x-mat.max.x;
            Assert.Less(gap,.8f,$"COURIER DOCK hangs over bare floor {gap:0.0} m from the ringed mat: the first sign in town sits on nothing");
            Assert.Greater(gap,0,"COURIER DOCK is printed across the mat's rings");
            Assert.Less(Mathf.Abs(sign.transform.position.z-mat.center.z),mat.extents.z,"COURIER DOCK lies past the mat's corner, not against its side");
        }

        // ---------------------------------------------------------------- item 8: the repair spot's marks and its light
        [UnityTest] public IEnumerator TallowsRepairSpotHasOneWrench()
        {
            yield return Zone("TallowDrift");
            Assert.AreEqual(1,All<Transform>().Count(t=>t.name=="Bay mark"),"fixture: the cradle's own mark on its pad");
            Assert.IsEmpty(All<MeshRenderer>().Where(r=>r.name=="Floor detail PictoRepair"),"a second wrench-and-gear mark lies on bare floor a step from the one on the cradle's pad: it reads as a leftover copy");
        }
        /// <summary>The particle shader ignores tiling, so a line drawn on the flare is the flare: one thin spark halfway up its length. The bay's soft column of light had only ever been a tiny cyan tick hanging in the air over the console, and the survey key's the same.</summary>
        [UnityTest] public IEnumerator ABaysLightIsAColumnNotASpark()
        {
            foreach(var zone in new[]{"TallowDrift","SunkenRelay","Sorrel_Ridges"})
            {
                yield return Zone(zone);
                var lights=All<LineRenderer>().Where(l=>l.name=="Bay light"||l.name=="Key light").ToArray();Assert.IsNotEmpty(lights,zone+": fixture: the bay's light");
                if(zone=="Sorrel_Ridges")Assert.IsTrue(lights.Any(l=>l.name=="Key light"),"fixture: the survey key's light");
                foreach(var light in lights)
                {
                    Assert.AreEqual("curtain_soft",light.sharedMaterial.GetTexture("_BaseMap").name,zone+": drawn on the flare, the bay's light is a tiny tick hanging over the console, attached to nothing");
                    Assert.AreEqual((float)BlendMode.One,light.sharedMaterial.GetFloat("_DstBlend"),zone+": the bay's light covers what's behind it instead of glowing over it");
                    Assert.Less(light.endColor.a,.03f,zone+": the bay's light ends in a hard edge in the air");
                }
            }
        }

        // ---------------------------------------------------------------- item 9: the canyon's walls and their shadows
        /// <summary>The rows of pale dashes across the gorge's arrival, s13/184. Followed toward the sun, nearly every one reaches the rise at the gorge's south end, between z -18 and z -12, whose faces are turned from the sun, with no lit side anywhere behind them: the mesh ends at z -24. Cast from the lit side only, as in v4, that rise lays no shadow and the arrival stands in the sun, with no rows. Cast from both sides it darkened 88% of the view, and the shadow pass's normal bias opened its unshared triangles into four straight rows of dashes.</summary>
        [UnityTest] public IEnumerator TheCanyonsWallsCastFromTheirLitSide()
        {
            foreach(var zone in new[]{"LanternGorge","Lanternwick"})
            {
                yield return Zone(zone);
                // Since November 10 the landform's shadow comes from one welded caster, the collision heightfield, and the drawn walls only receive (E3-lanternwick-08):
                // the caster must still cast from its lit side only, and be welded, since a wall triangle with its own vertices is what the normal bias opened into dashes.
                var cliffs=All<MeshFilter>().Single(f=>f.name.EndsWith(" cliffs")).GetComponent<MeshRenderer>();
                Assert.AreNotEqual(ShadowCastingMode.TwoSided,cliffs.shadowCastingMode,zone+": a wall casting from its back lays a shadow its mesh has no lit side for");
                var caster=All<MeshFilter>().Single(f=>f.name.EndsWith(" shadow")&&f.transform.parent==cliffs.transform.parent);var casts=caster.GetComponent<MeshRenderer>();
                Assert.AreEqual(ShadowCastingMode.ShadowsOnly,casts.shadowCastingMode,zone+": the landform's one caster casts from its lit side only");
                Assert.Less(caster.sharedMesh.vertexCount*3,(int)caster.sharedMesh.GetIndexCount(0),zone+": the caster's triangles share their vertices, so the shadow pass's normal bias can't open gaps between them");
                foreach(var drawn in new[]{cliffs,All<MeshFilter>().Single(f=>f.name.EndsWith(" floor")&&f.transform.parent==cliffs.transform.parent).GetComponent<MeshRenderer>()})
                    Assert.AreEqual(ShadowCastingMode.Off,drawn.shadowCastingMode,zone+": "+drawn.name+" casts as well as the welded caster, and its own unwelded triangles bring the dashes back");
            }
        }

        // ---------------------------------------------------------------- item 10: the dead rig
        [UnityTest] public IEnumerator TheRigsScorchIsAFaintStainBesideIt()
        {
            yield return Zone("Sorrel_Ridges");
            var scorch=All<MeshRenderer>().First(r=>r.name=="Rig scorch");
            Assert.Less(scorch.sharedMaterial.GetColor("_BaseColor").a,.4f,"a dark oval lies on the bare ground beside the rig with nothing above it to cast it: it reads as a hole or a stain with no source");
            Assert.Less(Mathf.Max(scorch.bounds.size.x,scorch.bounds.size.z),6.5f,"the rig's scorch runs half the length of its footing, a third of the screen");
        }
        [UnityTest] public IEnumerator TheDeadRigsStripsAreDark()
        {
            yield return Zone("Sorrel_Ridges");
            var rig=All<Transform>().First(t=>t.name=="OutpostDrill"&&t.parent==null);var dead=rig.GetComponent<StaysDark>();
            Assert.IsNotNull(dead,"fixture: the rig stays dark");
            Assert.IsNotNull(dead.glass,"the rig's strips are painted bright green as well as lit: with the light off they still say a dead machine is running");
            var block=new MaterialPropertyBlock();rig.GetComponentInChildren<Renderer>().GetPropertyBlock(block);
            Assert.AreEqual(dead.glass,block.GetTexture("_BaseMap"),"the rig doesn't wear its dead paint");
        }

        // ---------------------------------------------------------------- item 11: the Great Lantern's seal
        /// <summary>The cloud behind the party as Hessa's fight starts is the seal coming on: strands overlapping across the floor from x 3 to x 7, at a curtain's full strength and in her markings' orange. No lamp moved. It's a door of relay light, so it's that colour, at half the strength or less.</summary>
        [UnityTest] public IEnumerator TheSealIsLanternLightNotAnOrangeCloud()
        {
            yield return Zone("SunkenRelay");
            var seal=All<Membrane>().Single(m=>m.name=="Great Lantern seal");var strand=seal.GetComponentsInChildren<LineRenderer>(true).First();
            var foot=strand.startColor;var strength=strand.sharedMaterial.GetColor("_BaseColor");
            Assert.Greater(foot.g/foot.r,.7f,"the seal burns in the orange of Hessa's own floor markers: it reads as fire or a hazard behind the party");
            Assert.Less(foot.a*foot.r*strength.r,.5f,"the seal is the brightest thing on the screen, a saturated cloud across the floor with no lamp in view");
        }

        // ---------------------------------------------------------------- item 12: the nearest prop at the gorge's arrival
        [UnityTest] public IEnumerator TheGorgeHasNoFlatTealSlabs()
        {
            yield return Zone("LanternGorge");
            var slabs=All<Transform>().Where(t=>t.name=="CrystalClusterA"&&t.parent==null).Select(t=>t.position).ToArray();
            Assert.IsEmpty(slabs,"this kit's crystals are flat fills of teal on the chart: on a canyon floor, the first thing at the arrival, they read as untextured slabs on a mound, at "+string.Join(", ",slabs));
        }

        // ---------------------------------------------------------------- item 13: SALVAGE EDDY
        /// <summary>'SALVAGE EDD': the flight camera draws back through this chamber's fight, and SignFade holds a sign's size on the screen by growing it, here to nearly twice over. The bed rises from the tube's middle, so a sign laid 8 m off it rose into the bed at its far end.</summary>
        [UnityTest] public IEnumerator TheEddysSignKeepsItsWordsWithTheViewDrawnBack()
        {
            yield return Zone("Gullet_Tunnel");
            var sign=All<TextMeshPro>().First(t=>t.text=="SALVAGE EDDY");var at=sign.transform.position;
            const float Held=1.93f;float half=sign.GetPreferredValues(sign.text).x*.5f*Held;
            foreach(int side in new[]{-1,1})
            {
                Assert.IsTrue(Physics.Raycast(new Vector3(at.x+side*half,at.y+6,at.z),Vector3.down,out var bed,20,~0,QueryTriggerInteraction.Ignore),"fixture: the bed under the word's end");
                Assert.Less(bed.point.y,at.y,"with the view drawn back the bed rises through the end of the word: it reads 'SALVAGE EDD'");
            }
        }

        // ---------------------------------------------------------------- item 14: rubble in full sun
        [UnityTest] public IEnumerator TheRubbleByTheWrecksIsBurnedNotBrick()
        {
            yield return Zone("Sorrel_Ridges");
            var heaps=All<Transform>().Where(t=>t.name=="HushwellRubble"&&t.parent!=null&&(t.parent.name=="Thrown parts"||t.parent.name=="Rig wreckage")).ToArray();
            Assert.GreaterOrEqual(heaps.Length,3,"fixture: the heaps by the Burrower's wreck and the rig");
            foreach(var heap in heaps)
            {
                Assert.IsTrue(heap.TryGetComponent<PropTint>(out var tint),$"the heap at ({heap.position.x:0.0}, {heap.position.z:0.0}) is flat-topped tan blocks with pale outlines in full sun: stacked bricks, brighter than the wreck it was thrown from");
                Assert.Less(tint.tint.grayscale,.6f,"the heap's tint leaves it brighter than spoil");
            }
        }

        // ---------------------------------------------------------------- item 16: the relay's ward
        static Telegraph WardOf(RelayLantern lantern)=>Telegraph.Live.First(t=>t!=null&&t.owner==lantern);
        [UnityTest] public IEnumerator AWardsRingIsTheLampsColourAndFaint()
        {
            yield return Zone("LanternGorge");
            foreach(var fight in All<EncounterVolume>())fight.enabled=false;
            var lantern=All<RelayLantern>().First(r=>r.flag=="gorge.relay1");Flag(lantern.flag);yield return new WaitForSecondsRealtime(.2f);
            var ring=WardOf(lantern);var block=new MaterialPropertyBlock();ring.GetComponent<Renderer>().GetPropertyBlock(block);var tone=block.GetColor("_BaseColor");
            Assert.Less(tone.a,.4f,"a ring that stays on the floor for the rest of the zone is as strong as a skill's");
            // Whichever side of the gamma curve the block keeps it on: .89 or .95 as it was.
            Assert.Less(tone.g/tone.r,.87f,"the ring is the cream of the party's own effects: a lasting boundary reads as a skill, with nothing to tie it to a lantern");
            Assert.Less(ring.Radius-ring.Inner,.2f,"the ring has got as wide as an attack's marking");
        }
        /// <summary>By what's drawn, along the ring's own band: the ring off and on in one frame, so nothing else on the floor differs.</summary>
        [UnityTest] public IEnumerator AWardsRingFallsAwayInwardAndIsStillThere()
        {
            yield return Zone("LanternGorge");
            foreach(var fight in All<EncounterVolume>())fight.enabled=false;
            var lantern=All<RelayLantern>().First(r=>r.flag=="gorge.relay1");var hub=lantern.transform.position;
            // West of the relay on open floor, with the ward's edge in view and the party off it.
            Put(Lead,CombatCover.Ground(hub+Vector3.left*6+Vector3.up*6)+Vector3.up*.05f);
            foreach(var member in PartyController.Current.members)if(member!=Lead)Put(member,CombatCover.Ground(hub+Vector3.left*6+Vector3.forward*2+Vector3.up*6)+Vector3.up*.05f);
            Flag(lantern.flag);yield return new WaitForSecondsRealtime(1.5f);
            var ring=WardOf(lantern);var at=ring.GetComponent<MeshFilter>().sharedMesh.vertices;int columns=at.Length/2;
            const int W=1920,H=1080;
            ring.Hide(true);var bare=Grab(W,H);ring.Hide(false);var lit=Grab(W,H);
            float Added(float share)
            {
                float sum=0;int count=0;
                for(int c=0;c<columns;c++)
                {
                    var view=Camera.main.WorldToViewportPoint(ring.transform.TransformPoint(Vector3.Lerp(at[c],at[columns+c],share)));
                    if(view.z<=0||view.x<.03f||view.x>.97f||view.y<.2f||view.y>.85f)continue;
                    int pixel=Mathf.FloorToInt(view.y*H)*W+Mathf.FloorToInt(view.x*W);
                    sum+=lit[pixel].grayscale-bare[pixel].grayscale;count++;
                }
                Assert.Greater(count,8,"fixture: the ward's edge is in view");
                return sum/count;
            }
            float inside=Added(.2f),line=Added(.8f);
            Assert.Greater(line,.02f,$"the ward's edge hardly shows ({line:0.000}): nobody can see where it ends");
            Assert.Less(inside,line*.6f,$"the ring is a flat band, as strong on its inner side ({inside:0.000}) as at its line ({line:0.000}): a debug circle, with no falloff to read as light");
        }
    }
}
