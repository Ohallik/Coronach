using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D393: floors and water that read as one repeated pattern, and the Terminator's veil and lamps (round four of the November 23 whole-game capture). Each case is
    /// what the capture showed, measured in the zone as the player loads it.</summary>
    public sealed class QaNov24FloorsTests
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
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");Assert.AreEqual(zone,SceneManager.GetActiveScene().name);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return null;
        }

        /// <summary>nov23 s21 280, 290 and 309: the Floodcrown's basin floor was interlocking olive and dark-green blots wall to wall. The damp layer's own texture is blots, and it
        /// was painted at half weight (where the shader cuts it) over the basin's outer ring. The bed is silt now, in an outline at the room's scale, with water standing in the
        /// lowest of it: no damp layer on the bed, a silt layer over between a fifth and four fifths of it, few points left near the cut, and a puddle by the middle.</summary>
        [UnityTest] public IEnumerator TheCanopyBasinsBedIsSiltRootAndLitterAndNotOneBlotPattern()
        {
            yield return Zone("WalkingCanopy","Basin");
            var floor=All<MeshFilter>().FirstOrDefault(f=>f.name.EndsWith(" floor")&&f.sharedMesh!=null&&f.GetComponent<Renderer>().sharedMaterial!=null&&f.GetComponent<Renderer>().sharedMaterial.HasProperty("_SplatMap1"));
            Assert.IsNotNull(floor,"fixture: the island's layered floor");var mesh=floor.sharedMesh;Assert.IsFalse(mesh.name.StartsWith("Combined"),"fixture: the floor's own mesh, not a static batch");
            var material=floor.GetComponent<Renderer>().sharedMaterial;
            string Layer(int i){var t=material.GetTexture("_SplatMap"+(i+1));return t!=null?t.name:"";}
            var weights=new List<Vector4>();mesh.GetUVs(2,weights);var points=mesh.vertices;Assert.AreEqual(points.Length,weights.Count,"fixture: the floor carries its painted weights");
            var damp=Enumerable.Range(0,3).Where(i=>Layer(i)=="ground-damp").ToArray();int silt=Enumerable.Range(0,3).Where(i=>Layer(i)=="ground-sand").DefaultIfEmpty(-1).First();
            var centre=new Vector2(24,52);int bed=0,silty=0,near=0;float dampMost=0;
            for(int i=0;i<points.Length;i++)
            {
                var p=floor.transform.TransformPoint(points[i]);if(Vector2.Distance(new Vector2(p.x,p.z),centre)>16.5f||p.y>-4.95f)continue;
                bed++;foreach(int d in damp)dampMost=Mathf.Max(dampMost,weights[i][d]);
                if(silt>=0){float w=weights[i][silt];if(w>.5f)silty++;if(w>.3f&&w<.7f)near++;}
            }
            Assert.Greater(bed,200,"fixture: the basin's bed in the floor mesh");
            Assert.Less(dampMost,.2f,$"the damp layer (blots) lies at {dampMost:0.00} on the basin's bed: at a half it is camouflage");
            Assert.GreaterOrEqual(silt,0,"the floor has no silt layer for the basin's bed (layers: "+Layer(0)+", "+Layer(1)+", "+Layer(2)+")");
            float share=(float)silty/bed,cut=(float)near/bed;
            Assert.That(share,Is.InRange(.2f,.8f),$"silt covers {share:P0} of the bed: one stuff wall to wall, not ground with places in it");
            Assert.Less(cut,.25f,$"{cut:P0} of the bed lies near the layer's cut, where the shader's noise breaks it into blots");
            int puddles=All<MeshRenderer>().Count(r=>r.name.StartsWith("Floor detail puddle")&&Vector2.Distance(new Vector2(r.bounds.center.x,r.bounds.center.z),centre)<12);
            Assert.GreaterOrEqual(puddles,1,"no water stands in the lowest of the drained basin");
        }

        /// <summary>nov23 s35/420 and s36/076: one diagonal scoring at one angle and pitch covered every band of the Frozen Host's dig floor. Read in the cut ice's own texture, window
        /// by window (64 px, 44 cm), which groove families darken it: neither 45 degree diagonal at the old pitch carries more than a fifth of it, a fifth or more carries none,
        /// and at least four families each carry some of it.</summary>
        [UnityTest] public IEnumerator TheDigsCutIceIsScoredBandByBandAndNotOneDiagonalWallToWall()
        {
            yield return null;
            var path=System.IO.Path.Combine(Application.dataPath,"_Project/Art/Generated/Ground/ground-host-cutice.png");Assert.IsTrue(System.IO.File.Exists(path),"fixture: "+path);
            var texture=new Texture2D(2,2);Assert.IsTrue(texture.LoadImage(System.IO.File.ReadAllBytes(path)),"fixture: the cut ice reads");
            int n=texture.width;var px=texture.GetPixels32();Object.Destroy(texture);
            (int a,int b)[] families={(19,-19),(19,19),(27,6),(-7,26),(12,-20),(9,-34),(10,8),(13,13),(5,-22),(4,-30)};
            const int W=64;int windows=0,bare=0;var carried=new int[families.Length];
            for(int wy=0;wy+W<=n;wy+=W)for(int wx=0;wx+W<=n;wx+=W)
            {
                windows++;bool any=false;
                for(int f=0;f<families.Length;f++)
                {
                    double dark=0,light=0;int nd=0,nl=0;
                    for(int y=wy;y<wy+W;y++)for(int x=wx;x<wx+W;x++)
                    {
                        float g=Mathf.Repeat((x+.5f)/n*families[f].a+(y+.5f)/n*families[f].b,1);var c=px[y*n+x];float t=(c.r+c.g+c.b)/3f;
                        if(g<.15f){dark+=t;nd++;}else if(g>.3f&&g<.95f){light+=t;nl++;}
                    }
                    if(nd>0&&nl>0&&light/nl-dark/nd>7){carried[f]++;any=true;}
                }
                if(!any)bare++;
            }
            string tally=string.Join(", ",families.Select((f,i)=>$"({f.a},{f.b}) {carried[i]}"))+$" bare {bare} of {windows}";
            float diagonal=Mathf.Max(carried[0],carried[1])/(float)windows;
            Assert.LessOrEqual(diagonal,.2f,$"the one diagonal scores {diagonal:P0} of the cut ice: {tally}");
            Assert.GreaterOrEqual(bare/(float)windows,.2f,$"only {bare} of {windows} windows of the cut ice are unscored: {tally}");
            Assert.GreaterOrEqual(carried.Count(k=>k>=windows*.04f),4,$"fewer than four groove families score the cut ice: {tally}");
        }

        /// <summary>nov23 s24 112 and 176: Driftreed's mire was a flat dark sheet. The licensed water as the player draws it (the sheet takes it over on load): an absorption of 16 met
        /// its deep colour in a hand of water, so nothing changed with depth; ripples at .55 under a reflection at half strength could not be seen from the play camera.
        /// Now its deep colour is not reached before half a metre of water, its shallows stand apart from its deep, it ripples and it takes the light.</summary>
        [UnityTest] public IEnumerator DriftreedsWaterChangesWithDepthAndRipplesAndTakesTheLight()
        {
            yield return Zone("Driftreed");
            var patch=All<BiomePatch>().FirstOrDefault(p=>p.resourceKey=="Driftreed-mire");Assert.IsNotNull(patch,"fixture: Driftreed's mire sheet");
            Assert.IsTrue(patch.UsingLocalArt,"fixture: the licensed water draws the mire, as in the player");
            var m=patch.GetComponent<Renderer>().sharedMaterial;
            float F(string name){Assert.IsTrue(m.HasProperty(name),"fixture: "+m.shader.name+" has "+name);return m.GetFloat(name);}
            float absorption=F("_DepthVertical"),reached=1-Mathf.Exp(-absorption*.5f);
            Assert.Less(reached,.92f,$"at an absorption of {absorption} the mire is {reached:P0} its deep colour in half a metre of water: one flat sheet");
            Color shallow=m.GetColor("_ShallowColor").gamma,deep=m.GetColor("_BaseColor").gamma;
            Assert.Greater(shallow.grayscale-deep.grayscale,.18f,$"its shallows ({shallow}) and its deep ({deep}) are one colour");
            Assert.GreaterOrEqual(F("_NormalStrength"),.8f,"ripples too faint to catch the light at the play camera's distance");
            Assert.GreaterOrEqual(F("_ReflectionStrength"),.65f,"the dusk and the lanterns too faint in it to show the ripples");
            Assert.GreaterOrEqual(F("_PointSpotLightReflectionStrength"),3f,"the lanterns make no glint on it");
        }

        /// <summary>nov23 s55/152 and s56/059: a pale see-through sheet of rime lay behind the crawler at the run's start, as wide as it and cut straight down both sides; and of the
        /// lamps by the engine car two were flat orange discs and one a pale block. Rime lies now only behind low things (nothing behind a car or a crawler), and every lamp is a glass
        /// alone in its renderer inside an iron housing, the engine car's each with a pool of light that is out until the car wakes.</summary>
        [UnityTest] public IEnumerator TheTerminatorsRimeLiesOnlyBehindLowThingsAndItsLampsAreLanterns()
        {
            yield return Zone("Terminator");
            var dawn=Object.FindFirstObjectByType<DawnLine>();Assert.IsNotNull(dawn,"fixture: the dawn line");
            // No rime in the shade at all is an answer too (nothing low enough casts where it may lie): the night side's slate stands.
            var frost=All<MeshFilter>().FirstOrDefault(f=>f.name=="Floor detail frost in the shade");
            var points=frost!=null?frost.sharedMesh.vertices:new Vector3[0];var tones=frost!=null?frost.sharedMesh.colors:new Color[0];var faults=new List<string>();int tall=0;
            foreach(var c in dawn.casters.Where(k=>k.height>2.2f))
            {
                tall++;float x0=c.min.x+.3f,x1=c.max.x-.3f;if(x1<=x0)continue;
                int lying=0;for(int i=0;i<points.Length;i++){var p=frost.transform.TransformPoint(points[i]);if(p.x>x0&&p.x<x1&&p.z<c.min.y-1.5f&&p.z>c.min.y-5&&tones[i].a>.15f)lying++;}
                float strip=(x1-x0)*3.5f/.25f;if(lying>strip*.1f)faults.Add($"{lying} rimed points in the {strip:0}-point strip behind a {c.height:0.0} m caster at ({c.min.x:0.0},{c.min.y:0.0})");
            }
            Assert.Greater(tall,0,"fixture: cars and crawlers cast");
            Assert.IsEmpty(faults,"rime lies as a sheet behind tall things:\n"+string.Join("\n",faults));
            var ride=Object.FindFirstObjectByType<TreadRide>();Assert.IsNotNull(ride,"fixture: the ride out");
            Assert.AreEqual(ride.lamps.Length,ride.lampLights.Length,"not every engine car lamp has its pool of light");
            foreach(var l in ride.lampLights){Assert.IsNotNull(l,"a pool of light missing");Assert.IsFalse(l.enabled,"an engine car lamp's pool is lit before the car wakes");}
            var lamps=ride.lamps.Concat(TreadCoupling.All.SelectMany(t=>t.lamp)).Where(r=>r!=null).ToArray();Assert.Greater(lamps.Length,4,"fixture: the engine car's lamps and the couplings'");
            var renderers=All<Renderer>();
            foreach(var lamp in lamps)
            {
                Assert.AreEqual(1,lamp.sharedMaterials.Length,lamp.name+"'s glass shares its renderer, so the colour it is lit with lights the whole lamp into one block");
                Assert.IsTrue(renderers.Any(r=>r.name==lamp.name+" housing"),lamp.name+" has no housing: a lit disc and not a lamp");
            }
        }

        /// <summary>D417 (nov24-whole s56/059): beside the engine car's amber lenses a bare white globe hung under one hood: the third coupling's lamp, mated, its pale light
        /// (.82, .86, .8) over the crawl's pale lamp glass (.72, .56, .34) under the dawn sun. At the most the sun near the line and the sky give a coupling's glass, lit as it waits
        /// and as it is mated, it is a colour (its brightest channel at least four tenths over its least), never near white. The capture's lamp is read first and must fail.</summary>
        [UnityTest] public IEnumerator EveryCouplingsLampIsAColourNeverAWhiteGlobe()
        {
            yield return Zone("Terminator");
            var dawn=Object.FindFirstObjectByType<DawnLine>();Assert.IsNotNull(dawn,"fixture: the dawn line");
            var sky=RenderSettings.ambientSkyColor;float most=Mathf.Max(dawn.intensityNear,dawn.sun!=null?dawn.sun.intensity:0)+Mathf.Max(sky.r,sky.g,sky.b);
            Color Brightest(Color glass,Color lit)=>new Color(glass.r*most+lit.r,glass.g*most+lit.g,glass.b*most+lit.b);
            bool Coloured(Color c){float hi=Mathf.Max(c.r,c.g,c.b),lo=Mathf.Min(c.r,c.g,c.b);return hi>0&&(hi-lo)/hi>=.4f;}
            Assert.IsFalse(Coloured(Brightest(new Color(.72f,.56f,.34f),new Color(.82f,.86f,.8f))),"fixture: the capture's mated lamp reads as a colour");
            var couplings=TreadCoupling.All.Where(c=>c!=null).ToList();Assert.GreaterOrEqual(couplings.Count,3,"fixture: the three couplings");
            var faults=new List<string>();
            foreach(var c in couplings)foreach(var lamp in c.lamp)
            {
                var glass=lamp.sharedMaterial.GetColor("_BaseColor");
                foreach(var (state,lit) in new[]{("waiting",c.waiting),("mated",c.mated)}){var b=Brightest(glass,lit);if(!Coloured(b))faults.Add($"coupling {c.index}'s lamp {state} is {b} at the most the light gives: near white");}
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
    }
}
