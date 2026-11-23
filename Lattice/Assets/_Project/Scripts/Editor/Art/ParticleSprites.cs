using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// A particle draws its texture on a square, so the texture has to come to nothing at every edge of it or the square shows. The light curtain's texture
    /// (<c>curtain_soft.png</c>, which <see cref="ZoneKit.Glow"/> gives a floor glow) fades top to bottom only and is solid to its left and right edges: on a strand that is right,
    /// on a particle it is a rectangle, and the Frozen Host's vent steam, ninety additive puffs of it, drew as a stack of white squares over a third of the screen
    /// (whole-game capture s35/096 and 097, November 20). No editor still had shown it: a still never runs a particle system. A particle whose texture isn't round is given
    /// <see cref="Puff"/>, a soft round puff, wherever a builder makes one (<see cref="ZoneKit.Drift"/>), and <see cref="RoundAll"/> did it once for every built scene's
    /// particles. EffectsBuildTests.EveryParticleTheGameDrawsIsRoundNeverASquare holds every particle the build draws to it.
    /// </summary>
    public static class ParticleSprites
    {
        public const string Puff="Assets/_Project/Art/Particles/puff_soft.png";
        /// <summary>The most a sprite may show at the very edge of its square (its alpha, or for an additive material its light) and still read as round. A side at a sixth or
        /// more draws as a square; the curtain shows all of it. (A Vaskar flame sheet's tips touch its top at .075, and are a flame.)</summary>
        public const float Edge=.15f;
        static readonly Dictionary<string,float> known=new();
        /// <summary>How much this material's texture shows at its square's edge, read from the texture's own file (0 to 1). A material with no texture is a full square.</summary>
        public static float EdgeOf(Material material)
        {
            if(material==null)return 1;
            var texture=material.HasProperty("_BaseMap")?material.GetTexture("_BaseMap"):material.HasProperty("_MainTex")?material.GetTexture("_MainTex"):null;
            if(texture==null)return 1;
            string path=AssetDatabase.GetAssetPath(texture);if(string.IsNullOrEmpty(path)||!File.Exists(path))return 0;
            bool additive=material.HasProperty("_DstBlend")&&Mathf.Approximately(material.GetFloat("_DstBlend"),(float)BlendMode.One);
            string key=path+(additive?"|add":"|blend");if(known.TryGetValue(key,out float cached))return cached;
            var read=new Texture2D(2,2,TextureFormat.RGBA32,false);float most=0;
            try
            {
                if(!read.LoadImage(File.ReadAllBytes(path)))return 0;
                int w=read.width,h=read.height;var p=read.GetPixels32();
                float Shows(Color32 c)=>additive?Mathf.Max(c.r,Mathf.Max(c.g,c.b))/255f*c.a/255f:c.a/255f;
                for(int band=0;band<2;band++)
                {
                    for(int x=0;x<w;x++)most=Mathf.Max(most,Mathf.Max(Shows(p[band*w+x]),Shows(p[(h-1-band)*w+x])));
                    for(int y=0;y<h;y++)most=Mathf.Max(most,Mathf.Max(Shows(p[y*w+band]),Shows(p[y*w+w-1-band])));
                }
            }
            finally{Object.DestroyImmediate(read);}
            known[key]=most;return most;
        }
        /// <summary>The most a drift may stack along one line of sight through it, on average: the light an additive drift adds, or the cover an alpha one lays. Over it the drift
        /// reads as a flat white shape, not as steam: the Frozen Host's vent columns, ninety puffs of a third's alpha in a box under two metres wide, stacked about 1.6 (s35/097).</summary>
        public const float MostStack=.6f;
        static readonly Dictionary<string,float> means=new();
        /// <summary>How much of its square a sprite shows on average (0 to 1), read from the texture's file as <see cref="EdgeOf"/> reads its edge.</summary>
        public static float MeanOf(Material material)
        {
            var texture=material!=null&&material.HasProperty("_BaseMap")?material.GetTexture("_BaseMap"):null;if(texture==null)return 1;
            string path=AssetDatabase.GetAssetPath(texture);if(string.IsNullOrEmpty(path)||!File.Exists(path))return 1;
            bool additive=material.HasProperty("_DstBlend")&&Mathf.Approximately(material.GetFloat("_DstBlend"),(float)BlendMode.One);
            string key=path+(additive?"|add":"|blend");if(means.TryGetValue(key,out float cached))return cached;
            var read=new Texture2D(2,2,TextureFormat.RGBA32,false);double sum=0;int n=1;
            try{if(read.LoadImage(File.ReadAllBytes(path))){var p=read.GetPixels32();n=p.Length;foreach(var c in p)sum+=additive?Mathf.Max(c.r,Mathf.Max(c.g,c.b))/255f*c.a/255f:c.a/255f;}else sum=1;}
            finally{Object.DestroyImmediate(read);}
            return means[key]=(float)(sum/n);
        }
        /// <summary>What a drift stacks along a line of sight through its box from the ground camera (40 degrees down), on average: how many of its puffs a line meets (their
        /// number, times a puff's area, over the box's area as the camera sees it), times what each shows (its alpha, its sprite's mean, its brightness if additive, and the
        /// three quarters of its life it is at full).</summary>
        public static float Stack(Material material,Color colour,int count,float small,float large,Vector3 size)
        {
            float area=Mathf.PI/4*(small*small+small*large+large*large)/3,seen=Mathf.Max(.01f,size.x*(size.z*Mathf.Sin(40*Mathf.Deg2Rad)+size.y*Mathf.Cos(40*Mathf.Deg2Rad)));
            bool additive=material!=null&&material.HasProperty("_DstBlend")&&Mathf.Approximately(material.GetFloat("_DstBlend"),(float)BlendMode.One);
            float bright=additive&&material.HasProperty("_BaseColor")?Mathf.Max(material.GetColor("_BaseColor").maxColorComponent*colour.maxColorComponent,0):1;
            return count*area/seen*colour.a*MeanOf(material)*bright*.75f;
        }
        /// <summary>A material a particle will draw with: if its texture would draw a square it is given the round puff, and said so. Returned for chaining.</summary>
        public static Material ForParticles(Material material)
        {
            if(material==null||!material.HasProperty("_BaseMap"))return material;
            var puff=AssetDatabase.LoadAssetAtPath<Texture2D>(Puff);if(puff==null)throw new System.Exception("PARTICLE_SPRITE_MISSING "+Puff);
            if(EdgeOf(material)>Edge)
            {
                var was=material.GetTexture("_BaseMap");
                material.SetTexture("_BaseMap",puff);EditorUtility.SetDirty(material);
                Debug.Log($"PARTICLE_SPRITE_ROUNDED {material.name}: {(was!=null?was.name:"no texture")} drew a square");
            }
            // A strand's glow drawn as puffs is vapour, not light: steam, smoke, mist and haze cover what is behind them and never add to it. Added, a few of them over snow or
            // pale stone already pass white (the pipeline has no tonemapper), and the vent columns' steam was a flat white plume.
            if(material.GetTexture("_BaseMap")==puff&&material.HasProperty("_DstBlend")&&Mathf.Approximately(material.GetFloat("_DstBlend"),(float)BlendMode.One))
            {
                material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);material.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
                if(material.HasProperty("_BaseColor")){var c=material.GetColor("_BaseColor");material.SetColor("_BaseColor",new Color(Mathf.Min(c.r,.9f),Mathf.Min(c.g,.9f),Mathf.Min(c.b,.9f),c.a));}
                EditorUtility.SetDirty(material);Debug.Log($"PARTICLE_VAPOUR {material.name}: drawn over what is behind it, not added to it");
            }
            return material;
        }
        static readonly Regex Block=new(@"\n--- !u!(\d+) ",RegexOptions.Compiled),Materials=new(@"m_Materials:\s*\n((?:\s*- \{[^}]*\}\s*\n)+)",RegexOptions.Compiled),
            Guid=new(@"guid: (\w+)",RegexOptions.Compiled),Mode=new(@"\n\s*m_RenderMode: (\d+)",RegexOptions.Compiled);
        /// <summary>Once, for the scenes already built: every material a built scene's particles draw with is given the round puff if it would draw a square, unless some other
        /// renderer (a strand, a mesh) draws with it too, which is left and named; one already given the puff is made vapour (<see cref="ForParticles"/>). Marker
        /// <c>PARTICLE_SPRITES_OK rounded=n shared=m</c>.</summary>
        public static void RoundAll()=>BatchTools.Run(()=>
        {
            var particles=new HashSet<string>();var others=new HashSet<string>();
            foreach(var scene in Directory.GetFiles("Assets/_Project/Scenes","*.unity"))
            {
                string text=File.ReadAllText(scene);var starts=Block.Matches(text).Cast<Match>().ToArray();
                for(int i=0;i<starts.Length;i++)
                {
                    int from=starts[i].Index,to=i+1<starts.Length?starts[i+1].Index:text.Length;string block=text.Substring(from,to-from),kind=starts[i].Groups[1].Value;
                    var list=Materials.Match(block);if(!list.Success)continue;
                    var guids=Guid.Matches(list.Groups[1].Value).Cast<Match>().Select(m=>m.Groups[1].Value);
                    var mode=Mode.Match(block);bool particle=kind=="199"&&!(mode.Success&&(mode.Groups[1].Value=="4"||mode.Groups[1].Value=="5"));
                    foreach(var g in guids)(particle?particles:others).Add(g);
                }
            }
            int rounded=0,shared=0;
            foreach(var guid in particles)
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);if(!path.StartsWith("Assets/")||!path.EndsWith(".mat"))continue;
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null)continue;
                bool square=EdgeOf(material)>Edge,puffed=material.HasProperty("_BaseMap")&&AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap"))==Puff;
                if(!square&&!puffed)continue;
                if(others.Contains(guid)){shared++;Debug.LogWarning("PARTICLE_SPRITE_SHARED "+path+": a particle and another renderer draw with it, so it is left as it is");continue;}
                ForParticles(material);rounded++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"PARTICLE_SPRITES_OK rounded={rounded} shared={shared}");
        });
    }
}
