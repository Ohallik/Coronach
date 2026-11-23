using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>One person, one face, and nobody else's (November 21, after the whole-game capture). The reviewers read Idra Sorn as two women (her own sheet at s44/064,
    /// her braced sheet at s48/061 and s46/020: a broader jaw, amber eyes where hers are silver, the visor reading as a crown), and asked whether any face was shared or
    /// recoloured (Orrin and Taren at s60/225 and s60/240, Ona and Dov at s27/038 and s25/066). These read the sheets themselves:
    /// <list type="bullet">
    /// <item>every speaker's sixteen expressions in a form come from one sheet, and a speaker shows one sheet in every form, but for the two heroes, whose shaped sheets
    /// are their natural faces in their frames (the same face, hair, markings and eyes under a visor's glow, looked at on November 21);</item>
    /// <item>no two speakers share a sheet, or show sheets so alike that one is the other recoloured: each expression's face (eyes to mouth, inside the hair line) as
    /// edges of its light and dark, which a recolour keeps, matched with the same expression of the other sheet over small shifts. A recolour scores about .97, the
    /// closest two different people the project has about .55 (one people drawn from one reference); the line is .8.</item>
    /// </list></summary>
    public sealed class PortraitSheetTests
    {
        static readonly string[] FormSheets={"Taren","Sela"};
        const int N=56;const float Recoloured=.8f;
        static CharacterDef[] Speakers()=>Resources.LoadAll<CharacterDef>("Definitions/CharacterDef").Where(c=>c.portraitNatural!=null).OrderBy(c=>c.name).ToArray();
        static IEnumerable<PortraitSet> Sets(CharacterDef c){yield return c.portraitNatural;if(c.portraitShaped!=null)yield return c.portraitShaped;}
        static IEnumerable<Texture2D> Sheets(PortraitSet set)=>set.entries.Where(e=>e.sprite!=null).Select(e=>e.sprite.texture).Distinct();

        [UnityTest] public IEnumerator EverySpeakerShowsOneFaceFromOneSheet()
        {
            yield return null;var faults=new List<string>();
            foreach(var c in Speakers())
            {
                foreach(var set in Sets(c))
                {
                    var sheets=Sheets(set).ToArray();
                    if(sheets.Length!=1)faults.Add($"{c.name}'s {set.name} mixes {sheets.Length} sheets: {string.Join(", ",sheets.Select(s=>s.name))}");
                    var missing=System.Enum.GetValues(typeof(PortraitEmotion)).Cast<PortraitEmotion>().Where(e=>!set.entries.Any(x=>x.emotion==e&&x.sprite!=null)).ToArray();
                    if(missing.Length>0)faults.Add($"{c.name}'s {set.name} has no {string.Join(", ",missing)}");
                }
                var all=Sets(c).SelectMany(Sheets).Distinct().ToArray();
                if(all.Length>1&&System.Array.IndexOf(FormSheets,c.name)<0)faults.Add($"{c.name} speaks with {all.Length} faces: {string.Join(" and ",all.Select(s=>s.name))}");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ---------------------------------------------------------------- the faces
        static readonly Dictionary<Texture2D,Texture2D> sources=new();static readonly Dictionary<Texture2D,Color32[]> pixels=new();
        /// <summary>The sheet as painted: its source file, full size (the imported texture is compressed and may be smaller).</summary>
        static Texture2D Source(Texture2D imported)
        {
            if(sources.TryGetValue(imported,out var known))return known;
            string path=Path.Combine(Application.dataPath,"_Project","Art","Portraits",imported.name+".png");
            if(!File.Exists(path))path=Directory.GetFiles(Application.dataPath,imported.name+".png",SearchOption.AllDirectories).FirstOrDefault();
            Assert.IsNotNull(path,"fixture: the source of "+imported.name);
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(path));sources[imported]=t;return t;
        }
        /// <summary>One expression's face as edges of its light and dark, (N+8) square: the cell's middle from the brow to the chin.</summary>
        static float[] Face(Color32[] pixels,int width,int height,Rect cell,System.Func<Color32,float> light)
        {
            int s=N+8;var lum=new float[s*s];
            for(int y=0;y<s;y++)for(int x=0;x<s;x++)
            {
                // Rows from the top of the cell down: .18 to .64 of it, across .27 to .73.
                float u0=cell.x+cell.width*(.27f+.46f*x/s),u1=cell.x+cell.width*(.27f+.46f*(x+1)/s);
                float v1=cell.yMax-cell.height*(.18f+.46f*y/s),v0=cell.yMax-cell.height*(.18f+.46f*(y+1)/s);
                float sum=0;int n=0;
                for(int py=Mathf.FloorToInt(v0);py<Mathf.CeilToInt(v1);py+=2)for(int px=Mathf.FloorToInt(u0);px<Mathf.CeilToInt(u1);px+=2)
                {if(px<0||py<0||px>=width||py>=height)continue;sum+=light(pixels[py*width+px]);n++;}
                lum[(s-1-y)*s+x]=n>0?sum/n:0;
            }
            var edges=new float[s*s];
            for(int y=1;y<s-1;y++)for(int x=1;x<s-1;x++){float gx=lum[y*s+x+1]-lum[y*s+x-1],gy=lum[(y+1)*s+x]-lum[(y-1)*s+x];edges[y*s+x]=Mathf.Sqrt(gx*gx+gy*gy);}
            return edges;
        }
        static float Light(Color32 c)=>(.299f*c.r+.587f*c.g+.114f*c.b)/255f;
        static Dictionary<PortraitEmotion,float[]> Faces(PortraitSet set,System.Func<Color32,float> light=null)
        {
            var faces=new Dictionary<PortraitEmotion,float[]>();
            foreach(var entry in set.entries.Where(e=>e.sprite!=null))
            {
                var source=Source(entry.sprite.texture);float k=source.width/(float)entry.sprite.texture.width;var r=entry.sprite.textureRect;
                if(!pixels.TryGetValue(source,out var all))pixels[source]=all=source.GetPixels32();
                faces[entry.emotion]=Face(all,source.width,source.height,new Rect(r.x*k,r.y*k,r.width*k,r.height*k),light??Light);
            }
            // A sheet is 21 MB read: let it go once its faces are taken.
            foreach(var t in sources.Values)Object.Destroy(t);sources.Clear();pixels.Clear();
            return faces;
        }
        /// <summary>The best match of b's face inside a's, over shifts of up to four cells each way.</summary>
        static float Match(float[] a,float[] b)
        {
            int s=N+8;double bm=0;for(int y=0;y<N;y++)for(int x=0;x<N;x++)bm+=b[(y+4)*s+x+4];bm/=N*N;
            double bn=0;for(int y=0;y<N;y++)for(int x=0;x<N;x++){double d=b[(y+4)*s+x+4]-bm;bn+=d*d;}
            float best=-1;
            for(int dy=0;dy<=8;dy+=2)for(int dx=0;dx<=8;dx+=2)
            {
                double am=0;for(int y=0;y<N;y++)for(int x=0;x<N;x++)am+=a[(y+dy)*s+x+dx];am/=N*N;
                double an=0,dot=0;for(int y=0;y<N;y++)for(int x=0;x<N;x++){double p=a[(y+dy)*s+x+dx]-am,q=b[(y+4)*s+x+4]-bm;an+=p*p;dot+=p*q;}
                best=Mathf.Max(best,(float)(dot/(System.Math.Sqrt(an*bn)+1e-9)));
            }
            return best;
        }
        static float Alike(Dictionary<PortraitEmotion,float[]> a,Dictionary<PortraitEmotion,float[]> b)=>a.Keys.Where(b.ContainsKey).Select(e=>Match(a[e],b[e])).DefaultIfEmpty(0).Average();

        [UnityTearDown] public IEnumerator Forget(){foreach(var t in sources.Values)Object.Destroy(t);sources.Clear();pixels.Clear();yield return null;}
        [UnityTest] public IEnumerator NoTwoSpeakersShareASheetOrARecolourOfOne()
        {
            yield return null;var speakers=Speakers();var faults=new List<string>();
            // One sheet per speaker for the comparison: the natural one (a form sheet is the same person's).
            var owner=new Dictionary<Texture2D,string>();
            foreach(var c in speakers)foreach(var sheet in Sets(c).SelectMany(Sheets).Distinct())
            {if(owner.TryGetValue(sheet,out var other)&&other!=c.name)faults.Add($"{c.name} and {other} speak from one sheet, {sheet.name}");else owner[sheet]=c.name;}
            var faces=speakers.ToDictionary(c=>c.name,c=>Faces(c.portraitNatural));
            // The measure can fail: a sheet against itself, and against itself recoloured (its colours turned round and darkened, as Orrin's against Taren's was read), are caught.
            var probe=speakers.FirstOrDefault(c=>c.name=="Orrin")??speakers[0];
            float self=Alike(faces[probe.name],faces[probe.name]);
            float recolour=Alike(faces[probe.name],Faces(probe.portraitNatural,c=>(.299f*c.b+.587f*c.r+.114f*c.g)*.8f/255f));
            TestContext.WriteLine($"PORTRAIT_FACES control: {probe.name} against itself {self:0.000}, against a recolour of itself {recolour:0.000}");
            Assert.Greater(self,.99f,"control: a sheet against itself is not the same face");
            Assert.GreaterOrEqual(recolour,Recoloured,$"control: {probe.name}'s sheet recoloured passes for another face ({recolour:0.000})");
            var scores=new List<(float score,string a,string b)>();
            for(int i=0;i<speakers.Length;i++)for(int j=i+1;j<speakers.Length;j++)scores.Add((Alike(faces[speakers[i].name],faces[speakers[j].name]),speakers[i].name,speakers[j].name));
            foreach(var (score,a,b) in scores.OrderByDescending(s=>s.score).Take(8))TestContext.WriteLine($"PORTRAIT_FACES {a} ~ {b} {score:0.000}");
            foreach(var (score,a,b) in scores.Where(s=>s.score>=Recoloured))faults.Add($"{a}'s face and {b}'s are one face recoloured ({score:0.000})");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
    }
}
