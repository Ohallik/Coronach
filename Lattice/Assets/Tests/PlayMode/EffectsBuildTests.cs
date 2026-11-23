using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>What the effects ask of the build, checked against what the build carries. The whole-game capture of November 20 found large flat white shapes in the player
    /// that no editor still had shown (an editor still never runs a particle system): a vent pocket's steam drawn as stacked white squares (s35/096, 097), because its puffs
    /// were drawn with the light curtain's texture, which fades top to bottom only and is solid to its left and right edges. These hold every particle the game draws to a
    /// round sprite, every shader a script asks for by name to one the build has, and every material the game makes while it runs to a shader, a keyword set and textures
    /// that a built player carries (a variant no material in the build enables is stripped, and a texture only an editor tool reached isn't there).</summary>
    public sealed class EffectsBuildTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();ImpactSettings.Reset();
            if(SceneManager.GetActiveScene().name!="_Boot"){SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        }

        // ------------------------------------------------------------------ what the build carries
        static HashSet<string> carriedPaths;static Dictionary<Shader,HashSet<string>> carried;static HashSet<Shader> always,carriedShaders;static string[] buildScenes;
        /// <summary>Every asset a built player carries: the build's scenes and everything in a Resources folder, with all they depend on; and the always-included shaders.</summary>
        static void Learn()
        {
            if(carried!=null)return;
            buildScenes=UnityEditor.EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if(buildScenes.Length==0)buildScenes=Directory.GetFiles(Path.Combine(Application.dataPath,"_Project/Scenes"),"*.unity").Select(p=>"Assets/_Project/Scenes/"+Path.GetFileName(p)).ToArray();
            var resources=Directory.GetDirectories(Application.dataPath,"Resources",SearchOption.AllDirectories).Select(d=>"Assets"+d.Substring(Application.dataPath.Length).Replace('\\','/')).ToArray();
            var roots=buildScenes.Concat(UnityEditor.AssetDatabase.FindAssets("",resources).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Where(p=>!UnityEditor.AssetDatabase.IsValidFolder(p))).Distinct().ToArray();
            carriedPaths=new HashSet<string>(UnityEditor.AssetDatabase.GetDependencies(roots,true));foreach(var r in roots)carriedPaths.Add(r);
            carried=new Dictionary<Shader,HashSet<string>>();carriedShaders=new HashSet<Shader>();always=new HashSet<Shader>();
            foreach(var path in carriedPaths)
            {
                if(path.EndsWith(".shader")){var s=UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(path);if(s!=null)carriedShaders.Add(s);continue;}
                if(!path.EndsWith(".mat"))continue;
                var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null||m.shader==null)continue;
                if(!carried.TryGetValue(m.shader,out var sets))carried[m.shader]=sets=new HashSet<string>();sets.Add(Keys(m));
            }
            var settings=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if(settings!=null)
            {
                var list=new UnityEditor.SerializedObject(settings).FindProperty("m_AlwaysIncludedShaders");
                for(int i=0;list!=null&&i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue is Shader s)always.Add(s);
            }
        }
        /// <summary>A material's keywords that pick a variant the build may strip: its shader_feature ones. A multi_compile keyword's variants are all compiled into the build
        /// whatever any material enables (the world signs' UNDERLAY_ON on Lattice/SignText is one), so it is left out of the comparison.</summary>
        static string Keys(Material m)=>string.Join(" ",m.enabledKeywords.Select(k=>k.name).Where(n=>!Compiled(m.shader).Contains(n)).OrderBy(n=>n,System.StringComparer.Ordinal));
        static readonly Dictionary<Shader,HashSet<string>> compiled=new();
        static readonly Regex MultiCompile=new(@"#pragma\s+multi_compile\w*\s+([^\r\n]*)",RegexOptions.Compiled);
        /// <summary>The keywords a shader's source declares with multi_compile (any of its variants), read from its file; none for a shader with no source to read.</summary>
        static HashSet<string> Compiled(Shader s)
        {
            if(s==null)return new HashSet<string>();
            if(compiled.TryGetValue(s,out var known))return known;
            known=new HashSet<string>();string path=UnityEditor.AssetDatabase.GetAssetPath(s);
            string full=string.IsNullOrEmpty(path)?null:Path.GetFullPath(path);
            if(full!=null&&File.Exists(full))foreach(Match m in MultiCompile.Matches(File.ReadAllText(full)))
                foreach(var word in m.Groups[1].Value.Split(new[]{' ','\t'},System.StringSplitOptions.RemoveEmptyEntries))if(word.Trim('_').Length>0)known.Add(word);
            return compiled[s]=known;
        }
        static bool Carries(Shader s)=>s!=null&&(always.Contains(s)||carriedShaders.Contains(s)||carried.ContainsKey(s));
        /// <summary>What is wrong with a material made at run time, for a built player: its shader isn't carried, its keywords make a variant no carried material enables, or a texture
        /// it draws is an asset the build doesn't carry. Null when nothing is.</summary>
        static string Fault(Material m)
        {
            if(m.shader==null)return "no shader";
            if(!Carries(m.shader))return "its shader "+m.shader.name+" isn't in the build";
            string keys=Keys(m);
            if(keys.Length>0&&!always.Contains(m.shader)&&!(carried.TryGetValue(m.shader,out var sets)&&sets.Contains(keys)))
                return $"its keywords [{keys}] on {m.shader.name} are a variant no material in the build enables (carried: {(carried.TryGetValue(m.shader,out var known)?string.Join(" | ",known.Select(k=>"["+k+"]")):"none")})";
            foreach(var name in m.GetTexturePropertyNames())
            {
                var t=m.GetTexture(name);if(t==null||!UnityEditor.EditorUtility.IsPersistent(t))continue;
                string path=UnityEditor.AssetDatabase.GetAssetPath(t);
                if(path.StartsWith("Assets/")&&!carriedPaths.Contains(path))return $"its {name} is {path}, which the build doesn't carry";
            }
            return null;
        }

        // ------------------------------------------------------------------ what a particle's sprite looks like at its edges
        /// <summary>How much a particle drawn with this material shows at the very edge of its square (0 to 1): its texture's alpha there, or for an additive material its light.
        /// A round sprite is nought all round; anything over a few hundredths draws a square.</summary>
        static float Edge(Material m,out string texture)
        {
            var tex=m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):m.HasProperty("_MainTex")?m.GetTexture("_MainTex"):null;
            texture=tex!=null?tex.name:"none";if(tex==null)return 1;
            var rt=RenderTexture.GetTemporary(tex.width,tex.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var active=RenderTexture.active;
            Color[] p;
            try{Graphics.Blit(tex,rt);RenderTexture.active=rt;var read=new Texture2D(tex.width,tex.height,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0);read.Apply();p=read.GetPixels();Object.Destroy(read);}
            finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
            int w=tex.width,h=tex.height;
            bool additive=m.HasProperty("_DstBlend")&&Mathf.Approximately(m.GetFloat("_DstBlend"),(float)BlendMode.One);
            bool premultiplied=!additive&&m.HasProperty("_SrcBlend")&&Mathf.Approximately(m.GetFloat("_SrcBlend"),(float)BlendMode.One);
            float Shows(Color c)=>additive?Mathf.Max(c.r,Mathf.Max(c.g,c.b))*c.a:premultiplied?Mathf.Max(c.a,Mathf.Max(c.r,Mathf.Max(c.g,c.b))):c.a;
            float most=0;
            for(int band=0;band<2;band++)
            {
                for(int x=0;x<w;x++){most=Mathf.Max(most,Shows(p[band*w+x]));most=Mathf.Max(most,Shows(p[(h-1-band)*w+x]));}
                for(int y=0;y<h;y++){most=Mathf.Max(most,Shows(p[y*w+band]));most=Mathf.Max(most,Shows(p[y*w+w-1-band]));}
            }
            return most;
        }
        const float Round=.15f;
        static readonly Regex RendererMode=new(@"\n\s*m_RenderMode: (\d+)",RegexOptions.Compiled),RendererMaterial=new(@"m_Materials:\s*\n\s*- \{fileID: (-?\d+)(?:, guid: (\w+))?",RegexOptions.Compiled);

        /// <summary>Every particle the game draws is round, never a square. Every particle renderer in the build's scenes (read from the scene files: no scene is opened), in every prefab
        /// the build carries and in every system the game makes as it runs through a fight (sparks, a swarm, a struck body) draws a sprite that shows under seven hundredths at the
        /// edges of its square. Mesh particles are their mesh's shape and aren't counted. The light curtain's own texture, which a vent's steam was drawn with, is shown to fail
        /// first.</summary>
        [UnityTest] public IEnumerator EveryParticleTheGameDrawsIsRoundNeverASquare()
        {
            Learn();
            // The fault first: the curtain's texture on a particle is a square (it fades top to bottom, and is solid to its left and right edges).
            var flare=Resources.Load<Material>("Effects/flare_01");Assert.IsNotNull(flare,"fixture: the shared flare");
            var curtain=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Particles/curtain_soft.png");Assert.IsNotNull(curtain,"fixture: the light curtain's texture");
            var wrong=new Material(flare){name="square fixture"};wrong.SetTexture("_BaseMap",curtain);
            Assert.Greater(Edge(wrong,out _),Round,"the check doesn't see a square: the curtain's texture passes as round");Object.Destroy(wrong);
            Assert.LessOrEqual(Edge(flare,out _),Round,"fixture: the shared flare is round");

            var failures=new List<string>();var seen=new HashSet<string>();int counted=0;
            void Judge(Material m,string where)
            {
                if(m==null){failures.Add(where+": a particle renderer with no material (a magenta or white square)");return;}
                counted++;if(!seen.Add(where+"|"+m.name))return;
                float edge=Edge(m,out string texture);if(edge>Round)failures.Add($"{where}: {m.name} ({texture}) shows {edge:0.00} at its square's edge");
            }
            foreach(var scene in buildScenes)
            {
                string text=File.ReadAllText(Path.Combine(Application.dataPath,"..",scene));
                foreach(var block in text.Split(new[]{"\n--- !u!"},System.StringSplitOptions.None).Where(b=>b.StartsWith("199 ")))
                {
                    var mode=RendererMode.Match(block);var material=RendererMaterial.Match(block);if(!material.Success)continue;
                    if(mode.Success&&(mode.Groups[1].Value=="4"||mode.Groups[1].Value=="5")||block.Contains("\n  m_Enabled: 0"))continue;
                    string guid=material.Groups[2].Value;
                    if(guid.Length==0){if(material.Groups[1].Value=="0")Judge(null,Path.GetFileNameWithoutExtension(scene));continue;}
                    var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                    if(m!=null)Judge(m,Path.GetFileNameWithoutExtension(scene));
                }
            }
            foreach(var path in carriedPaths.Where(p=>p.EndsWith(".prefab")))
            {
                var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)continue;
                foreach(var view in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    if(view.renderMode!=ParticleSystemRenderMode.Mesh&&view.renderMode!=ParticleSystemRenderMode.None)Judge(view.sharedMaterial,path);
            }
            // And what the game makes as it runs: a fight's sparks, a struck body, a swarm.
            yield return Fight();
            foreach(var view in Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None))
                if(view.renderMode!=ParticleSystemRenderMode.Mesh&&view.renderMode!=ParticleSystemRenderMode.None&&!UnityEditor.EditorUtility.IsPersistent(view.gameObject)&&view.gameObject.scene.name!="Arena_Ground")
                    Judge(view.sharedMaterial,"made at run time: "+view.name);
            string line=$"EFFECTS_PARTICLES renderers={counted} materials={seen.Count} scenes={buildScenes.Length} square={failures.Count}";TestContext.WriteLine(line);Debug.Log(line);
            Assert.Greater(counted,100,"fixture: the scenes' particles were read");
            Assert.IsEmpty(failures,line+"\n"+string.Join("\n",failures));
        }

        static readonly Regex AskedShader=new(@"Shader\.Find\(\s*""([^""]+)""\s*\)",RegexOptions.Compiled),AskedResource=new(@"Resources\.Load<(\w+)>\(\s*""([^""]+)""\s*\)(\s*\?\?\s*Resources\.Load<\w+>\(\s*""([^""]+)""\s*\))?",RegexOptions.Compiled);
        /// <summary>What the game's own scripts (not the editor's) ask for by name and the build doesn't carry: a shader Shader.Find won't find in a player, or a Resources path with
        /// nothing at it.</summary>
        static List<string> Unanswered(IEnumerable<(string file,string text)> sources)
        {
            var missing=new List<string>();
            foreach(var (file,text) in sources)
            {
                foreach(Match m in AskedShader.Matches(text)){var s=Shader.Find(m.Groups[1].Value);if(s==null||!Carries(s))missing.Add($"{file}: Shader.Find(\"{m.Groups[1].Value}\") {(s==null?"names no shader":"names a shader the build doesn't carry")}");}
                foreach(Match m in AskedResource.Matches(text))
                {
                    string kind=m.Groups[1].Value,path=m.Groups[2].Value;
                    var type=kind switch{"Material"=>typeof(Material),"Shader"=>typeof(Shader),"Texture2D"=>typeof(Texture2D),"Texture"=>typeof(Texture),"Sprite"=>typeof(Sprite),"GameObject"=>typeof(GameObject),
                        "AudioClip"=>typeof(AudioClip),"TextAsset"=>typeof(TextAsset),_=>typeof(Object)};
                    if(Resources.Load(path,type)!=null)continue;
                    // A name its script falls back from in the same expression (a ?? to another Resources path) is answered by the fallback, and said so.
                    if(m.Groups[4].Success&&Resources.Load(m.Groups[4].Value,type)!=null){TestContext.WriteLine($"EFFECTS_NAME_FALLBACK {file}: \"{path}\" finds nothing; its fallback \"{m.Groups[4].Value}\" answers");continue;}
                    missing.Add($"{file}: Resources.Load<{kind}>(\"{path}\") finds nothing");
                }
            }
            return missing;
        }
        /// <summary>Every shader and effect the game's scripts ask for by name is one the build carries. A name that is wrong, moved, or carried only by the editor answers in the editor
        /// and comes back null in a player (or, for Shader.Find, falls back to whatever the script falls back to). Shown to fail first on a script that names a shader and an effect
        /// that don't exist.</summary>
        [Test] public void EveryShaderAndEffectTheGamesScriptsAskForByNameIsOneTheBuildCarries()
        {
            Learn();
            var wrong=Unanswered(new[]{("fixture.cs","var a=Shader.Find(\"Lattice/NoSuchShader\");var b=Resources.Load<Material>(\"Effects/no_such_effect\");")});
            Assert.AreEqual(2,wrong.Count,"the check doesn't see a name the build can't answer: "+string.Join("; ",wrong));
            string scripts=Path.Combine(Application.dataPath,"_Project/Scripts");
            var sources=Directory.GetFiles(scripts,"*.cs",SearchOption.AllDirectories).Where(f=>!f.Replace('\\','/').Contains("/Editor/"))
                .Select(f=>(f.Substring(scripts.Length+1).Replace('\\','/'),File.ReadAllText(f))).ToArray();
            var missing=Unanswered(sources);
            string line=$"EFFECTS_NAMES scripts={sources.Length} unanswered={missing.Count}";TestContext.WriteLine(line);Debug.Log(line);
            Assert.IsEmpty(missing,string.Join("\n",missing));
        }

        /// <summary>A zone with a fight in it, and the effects a fight makes: a guardian's plate, a swarm's motes, a marked circle, a struck body's flash and sparks, an energy body.</summary>
        static IEnumerator Fight()
        {
            GameTime.Reset();ImpactSettings.Reset();ImpactSettings.Volatile=true;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"Arena_Ground never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.5f);
            var lead=PartyController.Current.Active;var at=lead.transform.position;
            var made=new List<EnemyBrain>();
            foreach(var (id,off) in new[]{("MusterPikeguard",new Vector3(4,0,4)),("FibreWisp",new Vector3(-4,0,4)),("Ridgehound",new Vector3(0,0,5))})
            {var def=GameCatalog.Find<EnemyDef>(id);if(def==null)continue;var e=ActorFactory.Enemy(def,at+off);e.Passive=true;made.Add(e);}
            yield return null;yield return null;
            Telegraph.Circle(at+new Vector3(0,0,8),2,3,Telegraph.Heavy);ActorFactory.Energy(new Color(.5f,1,1.2f,.9f));
            foreach(var e in made)if(e!=null&&e.Health.Alive)e.Health.Receive(new DamagePacket{source=lead.Health,amount=1,type=DamageType.Plasma,weight=HitWeight.Heavy});
            for(int i=0;i<6;i++)yield return null;
        }

        /// <summary>Every material the game makes while it runs is one a built player can draw: its shader is carried, its keywords are a variant some material in the build enables
        /// (a variant no material in the build enables is stripped: a keyword switched on at run time works in the editor and does nothing in a player, CHAPTER_GUIDE 4.3), and every
        /// texture it draws is carried. A zone is loaded and a fight's effects are made in it, and every material then alive that isn't an asset is held to it. Shown to fail first
        /// on a material made here with three of the world shader's keywords no material uses together.</summary>
        [UnityTest] public IEnumerator EveryMaterialTheGameMakesWhileItRunsIsOneABuiltPlayerCanDraw()
        {
            Learn();
            yield return Fight();
            var shader=Shader.Find("Lattice/Toon");Assert.IsNotNull(shader);
            var wrong=new Material(shader){name="variant fixture"};wrong.EnableKeyword("_ICE_ON");wrong.EnableKeyword("_GEL_ON");wrong.EnableKeyword("_WIND_ON");
            Assert.IsNotNull(Fault(wrong),"the check doesn't see a variant the build strips");Object.DestroyImmediate(wrong);
            var made=Resources.FindObjectsOfTypeAll<Material>().Where(m=>m!=null&&!UnityEditor.EditorUtility.IsPersistent(m)&&m.shader!=null&&!m.shader.name.StartsWith("Hidden/")).ToArray();
            var failures=made.Select(m=>(m,why:Fault(m))).Where(x=>x.why!=null).Select(x=>$"{x.m.name} ({x.m.shader.name}): {x.why}").Distinct().ToList();
            string line=$"EFFECTS_RUNTIME_MATERIALS made={made.Length} faults={failures.Count}";TestContext.WriteLine(line);Debug.Log(line);
            foreach(var m in made.Select(m=>m.name+" | "+m.shader.name+" | "+Keys(m)).Distinct().OrderBy(s=>s))TestContext.WriteLine("  "+m);
            Assert.Greater(made.Length,5,"fixture: the fight made materials");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
