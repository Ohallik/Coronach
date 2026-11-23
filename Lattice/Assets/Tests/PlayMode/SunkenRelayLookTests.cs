using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.UI;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The Sunken Relay as the whole-game capture of November 20 showed it (nov20-whole s15/049, 135, 148): from where its fights are fought, one plain dark floor, and as
    /// Hessa's fight opened nothing of the Great Lantern in frame. What can be measured from the game's own camera is measured here; each case puts the capture's state back first
    /// (the zone's old light, and nothing of what was laid in the floor or over it since) and requires the fault there, so a check that can't fail can't pass. The rest is judged on stills.</summary>
    public sealed class SunkenRelayLookTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void NeedsGraphics()=>Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"this reads real pixels: run it with the -Graphics runner");
        static IEnumerator Relay()
        {
            SceneFlow.Current.LoadZone("SunkenRelay","Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the Sunken Relay never finished loading");
            Camera.main.aspect=16f/9;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            // Nothing starts while the party is put about the zone: a fight's framing would move the camera these read through.
            foreach(var fight in All<EncounterVolume>())GameServices.Current.Flags.SetBool("clear."+fight.encounterId,true);
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at){var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        static IEnumerator Stand(Vector3 at)
        {
            var lead=PartyController.Current.Active;Put(lead,at+Vector3.up*.1f);
            // The partner a step behind on the west side: on the east she stood inside the cut corridor's wall.
            foreach(var member in PartyController.Current.members)if(member!=lead)Put(member,at+new Vector3(-2.2f,.1f,-2));
            yield return new WaitForSecondsRealtime(1.4f);
        }
        /// <summary>What the game's camera draws, without the HUD, at the capture's own size. Row 0 is the bottom of the screen.</summary>
        static Color[] Grab(int W=1920,int H=1080)
        {
            var camera=Camera.main;var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try{Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();return image.GetPixels();}
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
        }

        /// <summary>The largest stretch of plain floor in a frame, as a share of it, measured as tools/look_density.py measures a still (the bar is .22): the frame at 640 wide, its
        /// luminance blurred over five pixels so texture grain doesn't count, cut in 16-pixel tiles; a tile is plain when it has no edge (mean gradient under 9) and is the frame's
        /// commonest floor tone, and the answer is the largest 4-connected run of plain tiles.</summary>
        internal static float LargestPlain(Color[] px,int W,int H)
        {
            int k=W/640,w=640,h=H/k;var rgb=new Vector3[w*h];var lum=new float[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                var sum=Vector3.zero;for(int dy=0;dy<k;dy++)for(int dx=0;dx<k;dx++){var c=px[(H-1-(y*k+dy))*W+x*k+dx];sum+=new Vector3(c.r,c.g,c.b);}
                sum*=255f/(k*k);rgb[y*w+x]=sum;lum[y*w+x]=sum.x*.299f+sum.y*.587f+sum.z*.114f;
            }
            var blur=new float[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++){float s=0;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)s+=lum[Mathf.Clamp(y+dy,0,h-1)*w+Mathf.Clamp(x+dx,0,w-1)];blur[y*w+x]=s/25;}
            float At(int x,int y)=>blur[y*w+x];
            const int Tile=16;int nx=w/Tile,ny=h/Tile;var g=new float[nx*ny];var tone=new Vector3[nx*ny];
            for(int y=0;y<ny*Tile;y++)for(int x=0;x<nx*Tile;x++)
            {
                float gx=x==0?At(1,y)-At(0,y):x==w-1?At(x,y)-At(x-1,y):(At(x+1,y)-At(x-1,y))*.5f;
                float gy=y==0?At(x,1)-At(x,0):y==h-1?At(x,y)-At(x,y-1):(At(x,y+1)-At(x,y-1))*.5f;
                int t=(y/Tile)*nx+x/Tile;g[t]+=Mathf.Sqrt(gx*gx+gy*gy)*4/(Tile*Tile);tone[t]+=rgb[y*w+x]/(Tile*Tile);
            }
            var quiet=Enumerable.Range(0,nx*ny).Where(i=>g[i]<9).ToList();if(quiet.Count==0)return 0;
            var commonest=quiet.GroupBy(i=>(Mathf.FloorToInt(tone[i].x/24),Mathf.FloorToInt(tone[i].y/24),Mathf.FloorToInt(tone[i].z/24))).OrderByDescending(q=>q.Count()).First();
            var floor=commonest.Aggregate(Vector3.zero,(s,i)=>s+tone[i])/commonest.Count();
            var plain=new bool[nx*ny];foreach(int i in quiet)plain[i]=Vector3.Distance(tone[i],floor)<26;
            var seen=new bool[nx*ny];int best=0;var stack=new Stack<int>();
            for(int i=0;i<nx*ny;i++)
            {
                if(!plain[i]||seen[i])continue;int n=0;seen[i]=true;stack.Push(i);
                while(stack.Count>0)
                {
                    int a=stack.Pop();n++;int ax=a%nx,ay=a/nx;
                    foreach(var (bx,by) in new[]{(ax+1,ay),(ax-1,ay),(ax,ay+1),(ax,ay-1)})
                    {if(bx<0||by<0||bx>=nx||by>=ny)continue;int b=by*nx+bx;if(plain[b]&&!seen[b]){seen[b]=true;stack.Push(b);}}
                }
                best=Mathf.Max(best,n);
            }
            return best/(float)(nx*ny);
        }

        /// <summary>The relay as the capture had it: the zone's light of November 20 (sun .28 in a blue-violet, sky fill a little dimmer, the camera's exposure a third of a stop
        /// under today's) and nothing of what this pass laid in the floor or over it (the bronze bands and the lantern's spokes, the cables laid across the floors, the racks and
        /// fallen posts stood where the fights are seen, the moving light). Undone by disposing it.</summary>
        sealed class CapturedState:System.IDisposable
        {
            readonly List<System.Action> undo=new();
            public CapturedState()
            {
                var sun=All<Light>().Single(l=>l.type==LightType.Directional&&l.gameObject.scene.name=="SunkenRelay");
                var (color,intensity)=(sun.color,sun.intensity);sun.color=new Color(.62f,.66f,.85f);sun.intensity=.28f;undo.Add(()=>{sun.color=color;sun.intensity=intensity;});
                var (sky,equator,below)=(RenderSettings.ambientSkyColor,RenderSettings.ambientEquatorColor,RenderSettings.ambientGroundColor);
                RenderSettings.ambientSkyColor=new Color(.18f,.19f,.27f);RenderSettings.ambientEquatorColor=new Color(.27f,.27f,.33f);RenderSettings.ambientGroundColor=new Color(.16f,.15f,.17f);DynamicGI.UpdateEnvironment();
                undo.Add(()=>{RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=below;DynamicGI.UpdateEnvironment();});
                // The scene's own profile, instanced, so the asset is never touched; through reflection, as this assembly doesn't see the render pipeline's types.
                var volumeType=System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");Assert.IsNotNull(volumeType,"fixture: the post volume's type");
                bool Global(Object v)=>(bool)(volumeType.GetField("isGlobal")?.GetValue(v)??volumeType.GetProperty("isGlobal").GetValue(v));
                var volume=(Component)Object.FindObjectsByType(volumeType,FindObjectsSortMode.None).Single(v=>((Component)v).gameObject.scene.name=="SunkenRelay"&&Global(v));
                var profile=volumeType.GetProperty("profile").GetValue(volume);var adjust=((IEnumerable)profile.GetType().GetField("components").GetValue(profile)).Cast<object>().Single(c=>c.GetType().Name=="ColorAdjustments");
                foreach(var (field,then) in new[]{("postExposure",.25f),("contrast",12f)})
                {
                    var parameter=adjust.GetType().GetField(field).GetValue(adjust);var value=parameter.GetType().GetProperty("value");var now=value.GetValue(parameter);
                    value.SetValue(parameter,then);undo.Add(()=>value.SetValue(parameter,now));
                }
                foreach(var shade in All<GroundShade>().Where(s=>s.enabled)){shade.enabled=false;undo.Add(()=>shade.enabled=true);}
                foreach(var r in All<Renderer>().Where(r=>r.enabled&&New(r))){r.enabled=false;undo.Add(()=>r.enabled=true);}
            }
            static readonly HashSet<string> Laid=new(){"Relay floor inlay","Lantern spoke","Relay cable laid in","Lantern light on the floor","Lantern light in the water","Relay fight dressing"};
            static bool New(Renderer r){for(var t=r.transform;t!=null;t=t.parent)if(Laid.Contains(t.name))return true;return false;}
            public void Dispose(){for(int i=undo.Count-1;i>=0;i--)undo[i]();undo.Clear();}
        }

        /// <summary>s15/049, 135, 148: where the gallery's fight was seen from, and where Hessa's was, the frame was a plain dull floor with almost nothing on it. Every place the replay
        /// stood to fight, at the play camera's zoom, now measures as the lead measures a still: no plain stretch over .22 of the frame. On the capture's state each was well over.</summary>
        [UnityTest] public IEnumerator WhereTheRelaysFightsAreSeenNoStretchOfFloorIsPlain()
        {
            NeedsGraphics();yield return Relay();
            var spots=new[]{("the gallery (s15/049)",new Vector3(.1f,0,54.5f)),("the cut corridor",new Vector3(2.8f,0,33.8f)),("the vault",new Vector3(.2f,0,92.6f)),
                ("Hessa's fight as it opens (s15/135)",new Vector3(1.2f,0,124)),("Hessa's fight (s15/148)",new Vector3(7.1f,0,120.1f))};
            var then=new List<string>();var now=new List<string>();int plainThen=0;
            foreach(var (where,at) in spots)
            {
                yield return Stand(at);
                using(new CapturedState()){float share=LargestPlain(Grab(),1920,1080);then.Add($"{where} {share:0.00}");if(share>.22f)plainThen++;}
                yield return null;
                float shareNow=LargestPlain(Grab(),1920,1080);now.Add($"{where} {shareNow:0.00}");
            }
            TestContext.WriteLine("RELAY_PLAIN then: "+string.Join(", ",then)+"\nRELAY_PLAIN now: "+string.Join(", ",now));
            Assert.GreaterOrEqual(plainThen,4,"fixture: put back as the capture had it, the relay's fight grounds are plain floor again ("+string.Join(", ",then)+")");
            var plain=now.Where(n=>float.Parse(n.Substring(n.LastIndexOf(' ')+1),System.Globalization.CultureInfo.InvariantCulture)>.22f).ToArray();
            Assert.IsEmpty(plain,"where the relay's fights are seen a plain stretch of floor fills more than .22 of the frame: "+string.Join(", ",plain));
        }

        /// <summary>s15/135: as Hessa's fight opened, nothing of the Great Lantern was in frame, 34 m north of where she is fought, and the hall it is named for had two lamp pools in it.
        /// Its light is in frame now and is the brightest thing there: of the brightest pixels the camera draws, most are the lantern's (its beams crossing overhead, their light on the
        /// floor and in the water, the spokes that carry it), and on the capture's state none were.</summary>
        [UnityTest] public IEnumerator HessasFightOpensInTheGreatLanternsLight()
        {
            NeedsGraphics();yield return Relay();shares.Clear();
            foreach(var (where,at) in new[]{("s15/135",new Vector3(1.2f,0,124)),("s15/148",new Vector3(7.1f,0,120.1f))})
            {
                yield return Stand(at);
                float thenShare;
                using(new CapturedState()){var dark=Grab(960,540);var hidden=Hide();var darker=Grab(960,540);Show(hidden);thenShare=TheLanternsShare(dark,darker);}
                yield return null;
                var lit=Grab(960,540);var gone=Hide();var bare=Grab(960,540);Show(gone);
                float share=TheLanternsShare(lit,bare);
                TestContext.WriteLine($"RELAY_LANTERN {where} then={thenShare:0.00} now={share:0.00} camera={Camera.main.transform.position}");
                shares.Add((where,thenShare,share));
            }
            foreach(var (where,thenShare,share) in shares)
            {
                Assert.Less(thenShare,.5f,$"fixture: put back as the capture had it, at {where} the lantern's light is not what is brightest in frame ({thenShare:P0})");
                Assert.GreaterOrEqual(share,.5f,$"at {where}, as Hessa's fight opens, the Great Lantern's light is {share:P0} of the brightest pixels in frame: something else outshines the hall's own lantern");
            }
        }
        static readonly List<(string,float,float)> shares=new();
        /// <summary>Of the brightest half a percent of a frame's pixels, the share the lantern's light made: those that fall by a tenth or more without it.</summary>
        static float TheLanternsShare(Color[] frame,Color[] without)
        {
            var order=Enumerable.Range(0,frame.Length).OrderByDescending(i=>frame[i].grayscale).Take(Mathf.Max(1,frame.Length/200)).ToArray();
            return order.Count(i=>frame[i].grayscale-without[i].grayscale>.1f)/(float)order.Length;
        }
        /// <summary>The Great Lantern and every light it sends out: its glass, its beams, their light on the floor and in the water, the spokes of its hall's floor.</summary>
        static readonly string[] LanternsOwn={"Lantern beam","Lantern light on the floor","Lantern light in the water","Lantern spoke","GreatLantern"};
        static List<Renderer> Hide()
        {
            var hidden=new List<Renderer>();
            foreach(var r in All<Renderer>())
            {
                if(!r.enabled)continue;bool theirs=false;for(var t=r.transform;t!=null&&!theirs;t=t.parent)theirs=LanternsOwn.Contains(t.name);
                if(theirs){r.enabled=false;hidden.Add(r);}
            }
            return hidden;
        }
        static void Show(List<Renderer> hidden){foreach(var r in hidden)if(r!=null)r.enabled=true;}
    }
}
