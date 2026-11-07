using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass, how it looks and sounds where a test can see it: a place's air, the game's own type, and the
    /// last placeholder bodies a player could meet. Whether any of it is good is a question for eyes and ears.</summary>
    public sealed class QaLookAndSoundTests
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
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            if(PartyController.Current!=null)foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.4f);
        }
        static AudioSource Bed=>GameServices.Current.GetComponents<AudioSource>().Single(s=>s.loop&&s.outputAudioMixerGroup!=null&&s.outputAudioMixerGroup.name=="Ambience");
        static bool Blockout(Mesh mesh)=>mesh!=null&&(mesh.name.StartsWith("Sphere")||mesh.name.StartsWith("Cylinder")||mesh.name.StartsWith("Cube")||mesh.name.StartsWith("Capsule"));
        static Bounds BoundsOf(GameObject body)
        {
            var renderers=body.GetComponentsInChildren<MeshRenderer>();Assert.IsNotEmpty(renderers,body.name+" has nothing to draw");
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);return bounds;
        }

        [UnityTest] public IEnumerator EachNaturalPlaceHasItsOwnAir()
        {
            var heard=new HashSet<string>();
            foreach(string zone in new[]{"Sorrel_Ridges","Hushwell","Lanternwick","LanternGorge","SunkenRelay"})
            {
                yield return Zone(zone);
                var bed=Bed;Assert.IsNotNull(bed.clip,zone+" is silent");
                Assert.AreNotEqual("computerNoise_000",bed.clip.name,zone+" hums like a server room: wind, water and a town all sound like the Decks");
                Assert.IsTrue(heard.Add(bed.clip.name),zone+" shares its air with another place: "+bed.clip.name);
                Assert.IsTrue(bed.isPlaying,zone+"'s bed isn't playing");
                Assert.AreEqual(ZoneController.Current.definition.ambientVolume,bed.volume,.001f,zone+" ignores the level measured for its bed");
                Assert.Less(bed.volume,1.01f);
            }
        }
        [UnityTest] public IEnumerator StationsAndSpaceKeepTheirHum()
        {
            yield return Zone("Hub_Decks");
            Assert.AreEqual("computerNoise_000",Bed.clip.name,"the Decks lost their room tone");Assert.AreEqual(.055f,Bed.volume,.001f);
            yield return Zone("Hub_CinderHalo","Outer");
            Assert.AreEqual("spaceEngineLow_000",Bed.clip.name,"open space lost its engine hum");Assert.AreEqual(.055f,Bed.volume,.001f);
        }
        [UnityTest] public IEnumerator TheGameSpeaksInItsOwnType()
        {
            yield return null;
            Assert.AreEqual("Nunito SDF",TMP_Settings.defaultFontAsset.name,"every label is still the engine's default font");
            Assert.AreEqual("Cinzel SDF",UiSkin.Display.name,"headings have no display face, and the title is plain text");
            const string used="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?'\"()-+/%·×–—";
            foreach(var font in new[]{TMP_Settings.defaultFontAsset,UiSkin.Display})
            {
                foreach(char c in used)Assert.IsTrue(font.HasCharacter(c,true,false),font.name+" draws an empty box for '"+c+"'");
                Assert.IsFalse(font.fallbackFontAssetTable.Any(f=>f==null),font.name+" has a broken fallback");
                Assert.IsNotNull(font.material,font.name+" has no material");
                Assert.AreEqual("TextMeshPro/Distance Field",font.material.shader.name,font.name+" draws no outline: the mobile shader ignores outlineWidth, and the HUD's unbacked words wash out on Sorrel's ground");
            }
        }
        [UnityTest] public IEnumerator TheTitleSitsInItsBox()
        {
            // Boot opens on the title.
            float deadline=Time.unscaledTime+20;
            while(Object.FindFirstObjectByType<TitleScreen>()==null&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            var title=Object.FindObjectsByType<UnityEngine.UI.RawImage>(FindObjectsSortMode.None).SingleOrDefault(t=>t.name=="Title"&&t.transform.parent.name=="MainMenu");
            Assert.IsNotNull(title,"the logo is live text in a display face: a heading, not a wordmark");
            Assert.IsNotNull(title.texture,"the wordmark's art is missing");
            var rect=title.rectTransform.rect;
            Assert.AreEqual((float)title.texture.width/title.texture.height,rect.width/rect.height,.05f,"the wordmark is stretched out of its own proportions");
            // Inside the screen, and clear of the line under it.
            var corners=new Vector3[4];title.rectTransform.GetWorldCorners(corners);var canvas=title.canvas.GetComponent<RectTransform>();
            var low=canvas.InverseTransformPoint(corners[0]);var high=canvas.InverseTransformPoint(corners[2]);var box=canvas.rect;
            Assert.IsTrue(low.x>=box.xMin&&high.x<=box.xMax&&low.y>=box.yMin&&high.y<=box.yMax,$"the wordmark runs off the screen: {low} to {high} in {box}");
            var subtitle=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Single(t=>t.name=="Subtitle");subtitle.rectTransform.GetWorldCorners(corners);
            Assert.GreaterOrEqual(low.y,canvas.InverseTransformPoint(corners[1]).y,"the wordmark sits on the line under it");
        }
        [UnityTest] public IEnumerator EveryPageHeadingFitsInTheDisplayFace()
        {
            yield return Zone("Arena_Ground");
            var menu=Object.FindFirstObjectByType<PauseMenu>();
            for(int page=0;page<menu.PageCount;page++)
            {
                menu.Open(page);yield return null;
                var heading=menu.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Heading");
                Assert.AreEqual("Cinzel SDF",heading.font.name,heading.text+" isn't in the display face");
                Assert.LessOrEqual(heading.GetPreferredValues(heading.text,float.PositiveInfinity,float.PositiveInfinity).x,heading.rectTransform.rect.width,heading.text+" runs out of its box");
                menu.Close();yield return null;
            }
        }
        [UnityTest] public IEnumerator SignsAreInTheDisplayFace()
        {
            yield return Zone("Lanternwick");
            var signs=Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Sign_")).ToArray();
            Assert.IsNotEmpty(signs,"nothing to check");
            foreach(var sign in signs)
            {
                Assert.AreEqual("Cinzel SDF",sign.font.name,sign.text+" is still in the engine's default font");
                Assert.GreaterOrEqual(sign.fontSharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth),.22f,sign.text+" has no edge: pale type on a bright planet or a sunlit floor can't be read");
            }
        }
        [UnityTest] public IEnumerator RidgeCrystalsShowTheirFacets()
        {
            yield return Zone("Sorrel_Ridges");
            int looked=0;
            foreach(var node in Object.FindObjectsByType<Lattice.World.MineNode>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                foreach(var renderer in node.GetComponentsInChildren<Renderer>(true))
                {
                    var material=renderer.sharedMaterial;if(material==null||!material.HasProperty("_EmissionColor"))continue;
                    looked++;var glow=material.GetColor("_EmissionColor");
                    Assert.Less(glow.maxColorComponent,.8f,material.name+" glows past its own lighting: at "+glow+" every face reads the same flat cyan, with no facets");
                    Assert.Greater(glow.maxColorComponent,.2f,material.name+" stopped glowing: a crystal worth mining should still catch the eye");
                }
            Assert.Greater(looked,0,"fixture: Sorrel has crystals to mine");
        }
        [UnityTest] public IEnumerator TheSurveyDroneIsAMachineThatLooksWhereItShoots()
        {
            yield return Zone("Arena_Ground");
            var sela=PartyController.Current.members.Single(m=>m.character=="Sela");
            var drone=SurveyDrone.Deploy(sela,1);yield return null;
            foreach(var filter in drone.GetComponentsInChildren<MeshFilter>())Assert.IsFalse(Blockout(filter.sharedMesh),"the drone is still a ball and a disc: "+filter.sharedMesh.name);
            foreach(var renderer in drone.GetComponentsInChildren<Renderer>())Assert.IsFalse(renderer.sharedMaterial.name.StartsWith("Emission")||renderer.sharedMaterial.name=="Sela","the drone still wears a blockout material");
            Assert.IsEmpty(drone.GetComponentsInChildren<Collider>(),"the drone can catch the party");
            var size=BoundsOf(drone.gameObject).size;
            Assert.Greater(size.y,.3f,"too small to read from the ground camera");Assert.Less(size.y,1.2f,"bigger than a hero's torso");
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),sela.transform.position+new Vector3(6,0,3));enemy.Passive=true;
            yield return new WaitForSeconds(1.2f);
            var to=enemy.transform.position+Vector3.up*.8f-drone.transform.position;
            Assert.Less(Vector3.Angle(drone.Facing,to),25,"its lens looks somewhere other than what it's shooting at");
            float yaw=drone.Facing.x;yield return new WaitForSeconds(.3f);
            Assert.AreEqual(yaw,drone.Facing.x,.2f,"it spins like a top with a lens on it");
            Object.Destroy(enemy.gameObject);Object.Destroy(drone.gameObject);
        }
        [UnityTest] public IEnumerator SpoilIsRockAndBubblesAndSeedsAreEnergy()
        {
            yield return Zone("Arena_Ground");
            var hero=PartyController.Current.Active;var at=hero.transform.position;
            var rig=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),at+new Vector3(0,0,12)).GetComponent<BossController>();yield return null;
            rig.GetComponent<EnemyBrain>().Passive=true;yield return null;
            Assert.IsTrue(rig.Perform("SpoilToss"));yield return null;yield return null;
            var clumps=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Spoil clump").ToArray();
            Assert.IsNotEmpty(clumps,"the toss threw nothing to look at");
            foreach(var clump in clumps)
            {
                foreach(var filter in clump.GetComponentsInChildren<MeshFilter>())Assert.IsFalse(Blockout(filter.sharedMesh),"spoil is a grey ball");
                var size=BoundsOf(clump.gameObject).size;Assert.Greater(size.magnitude,.5f,"too small to see coming");Assert.Less(size.magnitude,3f,"a boulder, not a clump");
                Assert.IsEmpty(clump.GetComponentsInChildren<Collider>(),"a thrown clump could shove the party");
            }
            float deadline=Time.unscaledTime+5;while(rig.Busy&&Time.unscaledTime<deadline)yield return null;
            Object.Destroy(rig.gameObject);yield return null;
            var bellows=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),at+new Vector3(0,0,12)).GetComponent<BossController>();yield return null;
            bellows.GetComponent<EnemyBrain>().Passive=true;yield return null;
            Assert.IsTrue(bellows.Perform("Bubbles"));yield return null;yield return null;
            var bubbles=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Pressure bubble").ToArray();
            Assert.IsNotEmpty(bubbles,"no bubble to look at");
            foreach(var bubble in bubbles)Assert.AreEqual("Lattice/EnergyBubble",bubble.GetComponent<Renderer>().sharedMaterial.shader.name,"a pressure bubble is an orange ball");
            Object.Destroy(bellows.gameObject);yield return null;
            var sela=PartyController.Current.members.Single(m=>m.character=="Sela");
            var well=GravityWell.Throw(sela,sela.transform.position+Vector3.up,sela.transform.position+Vector3.forward*4,false,1);yield return null;
            Assert.AreEqual("Lattice/EnergyBubble",well.GetComponentInChildren<Renderer>().sharedMaterial.shader.name,"the Fold's seed is a flat glowing ball");
            Object.Destroy(well.gameObject);
        }
    }
}
