using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Lattice.UI
{
    /// <summary>
    /// Generated UI atlas slices and the permitted TMP font. Missing art falls
    /// back to flat colors without preventing navigation.
    /// </summary>
    public static class UiSkin
    {
        static readonly Dictionary<string, Sprite> Cache = new();
        static TMP_FontAsset _display;
        static bool _displaySearched;

        // ---------- sprites ----------

        public static readonly string[] Items={"ScrapAlloy","LatticeFilament","RidgeCrystal","HuskCore","ChoirResin","CantorPearl","Emitter","Edge","Frame","Drive","Module","RepairGel","ChargeCell","WarpKey","Scrip","Anvil"};
        public static Sprite Kit(string name)
        {
            // The sheet leaves 56 empty pixels under the panel and 6 over it. The slice used to carry them all, and its border then ended inside the
            // bottom bevel: every frame's art stood high in its rect, and whatever was centred in the rect sat low in the art.
            if(name=="panel_frame"||name=="panel_dark")return Slice("ui-panel",new(.02f,.0738f,.96f,.8662f),"panel",new(85,85,85,85),200);
            if(name.StartsWith("button")||name.StartsWith("row"))return Slice("ui-button",new(.02f,.25f,.96f,.54f),"button",new(160,90,160,90),300);
            if(name=="bar_frame"||name=="bar_fill")return Slice("ui-controls",new(.022f,.42f,.347f,.16f),"bar",new(60,20,60,20),100);
            if(name=="keycap")return Slice("ui-controls",new(.447f,.32f,.135f,.355f),"thumb",Vector4.zero,100);
            if(name=="reticle")return Slice("ui-controls",new(.69f,.14f,.29f,.76f),"reticle",Vector4.zero,100);
            return null;
        }
        public static Sprite Icon(string name)
        {
            int i=System.Array.IndexOf(Items,name);if(i<0)return null;
            float[] left={20,325,623,936},right={302,606,923,1235};
            float[] top={20,325,600,910},bottom={300,590,885,1190};
            int x=i%4,y=i/4;
            return Slice("item-icons",new(left[x]/1254,1-bottom[y]/1254,(right[x]-left[x])/1254,(bottom[y]-top[y])/1254),name,Vector4.zero,100);
        }
        public static Sprite Skill(string character,int slot)
        {return Slice("skill-icons",new(slot/4f,character=="Taren"?.5f:0,.25f,.5f),character+slot,Vector4.zero,100);}
        /// <summary>A skill's own icon: a 4x2 generated sheet, cells 0-3 on the top row and 4-7 below (D201).</summary>
        public static Sprite Skill(Lattice.Data.SkillDef skill)
        {
            if(skill==null)return null;int cell=Mathf.Clamp(skill.iconCell,0,7);string sheet=string.IsNullOrEmpty(skill.iconSheet)?"skill-icons":skill.iconSheet;
            return Slice(sheet,new((cell%4)/4f,cell<4?.5f:0,.25f,.5f),sheet+"#"+cell,Vector4.zero,100);
        }
        static Sprite Slice(string atlas,Rect normalized,string id,Vector4 border,float ppu)
        {
            if(Cache.TryGetValue(id,out var sprite))return sprite;
            var texture=Resources.Load<Texture2D>("UI/Generated/"+atlas);if(texture==null)return null;
            var rect=new Rect(normalized.x*texture.width,normalized.y*texture.height,normalized.width*texture.width,normalized.height*texture.height);
            sprite=Sprite.Create(texture,rect,new(.5f,.5f),ppu,0,SpriteMeshType.FullRect,border);sprite.name=id;Cache[id]=sprite;return sprite;
        }
        /// <summary>POLISH-05 full-screen art: the illustrated map set (Resources/UI/Map).</summary>
        public static Sprite Map(string name) => Load("UI/Map/", name);
        /// <summary>POLISH-05: the designed wordmark set (Resources/UI/Title).</summary>
        public static Sprite Title(string name) => Load("UI/Title/", name);
        /// <summary>POLISH-05: credits-roll art (Resources/UI/Credits).</summary>
        public static Sprite Credits(string name) => Load("UI/Credits/", name);

        static Sprite Load(string dir, string name)
        {
            string key = dir + name;
            if (Cache.TryGetValue(key, out var sprite))
                return sprite;
            sprite = Resources.Load<Sprite>(key);
            Cache[key] = sprite; // cache nulls too — missing art must not re-hit Resources
            return sprite;
        }

        // ---------- drawn here ----------

        /// <summary>A soft-edged block, nine-sliced, for a dark band behind words with no frame. A gradient, not art.</summary>
        public static Sprite SoftBand()
        {
            const string id="softband";const int size=64;
            if(Cache.TryGetValue(id,out var known)&&known!=null)return known;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float edge=Mathf.Min(Mathf.Min(x,size-1-x),Mathf.Min(y,size-1-y))/(size*.5f-1);
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255*Mathf.SmoothStep(0,1,edge)));
            }
            return Drawn(id,size,pixels,new Vector4(31,31,31,31));
        }
        /// <summary>The arrow's outline, tip up, in the sprite's -1 to 1. A long tip, narrow wings and a short blunt tail behind them, so it has one sharp end at any rotation. The wings used to reach further
        /// from the middle than the tip, and it read as a triangle aimed 120 degrees off. No notch, and the outline stays convex: at the size it's drawn a notch forked it into a V or a Y pointing down, and Y means Swap on the same screen, and up and left it was a desktop mouse pointer.</summary>
        public static readonly Vector2[] PointerOutline={new(0,.98f),new(.44f,-.46f),new(.24f,-.72f),new(-.24f,-.72f),new(-.44f,-.46f)};
        /// <summary>A solid arrowhead pointing up, for what the objective points at: drawn in code, not art.</summary>
        public static Sprite Pointer()
        {
            const string id="pointer";const int size=64,samples=4;
            if(Cache.TryGetValue(id,out var known)&&known!=null)return known;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int inside=0;
                for(int sy=0;sy<samples;sy++)for(int sx=0;sx<samples;sx++)
                {
                    var p=new Vector2((x+(sx+.5f)/samples)/size*2-1,(y+(sy+.5f)/samples)/size*2-1);
                    if(InPointer(p))inside++;
                }
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255*inside/(samples*samples)));
            }
            return Drawn(id,size,pixels,Vector4.zero);
        }
        /// <summary>Crossings counted along a line, which holds for whatever the outline is.</summary>
        static bool InPointer(Vector2 p)
        {
            bool inside=false;var shape=PointerOutline;
            for(int i=0,j=shape.Length-1;i<shape.Length;j=i++)
                if((shape[i].y>p.y)!=(shape[j].y>p.y)&&p.x<(shape[j].x-shape[i].x)*(p.y-shape[i].y)/(shape[j].y-shape[i].y)+shape[i].x)inside=!inside;
            return inside;
        }
        /// <summary>What lies over the right side of a bust, in the plate's own colour, so a shoulder goes into the box rather than stopping on a line. The seat's soft edge alone wasn't enough, and the reason is
        /// measured: the canvas blends in light and the eye reads in sRGB, so its straight ramp in light left a bright shawl at 66 levels four units from the cut, 47 at two and 19 at the cut. What this lets through falls by the eye's own curve, so what's seen falls evenly.
        /// It fades out upward as well: on all 288 cells the art only reaches the seat's edge below the chin, and above that it would lie across hair and cheeks for no gain.</summary>
        public static Sprite BustFade()
        {
            const string id="bustfade";const int wide=96,high=64;const float fullTo=.7f;
            if(Cache.TryGetValue(id,out var known)&&known!=null)return known;
            var pixels=new Color32[wide*high];
            for(int y=0;y<high;y++)for(int x=0;x<wide;x++)
            {
                float through=Mathf.Pow(1-x/(wide-1f),2.2f),rise=Mathf.Clamp01((y/(high-1f)-fullTo)/(1-fullTo));
                pixels[y*wide+x]=new Color32(255,255,255,(byte)(255*(1-through)*(1-Mathf.SmoothStep(0,1,rise))));
            }
            return Drawn(id,wide,high,pixels,Vector4.zero,true);
        }
        static Sprite Drawn(string id,int size,Color32[] pixels,Vector4 border)=>Drawn(id,size,size,pixels,border,false);
        static Sprite Drawn(string id,int wide,int high,Color32[] pixels,Vector4 border,bool readable)
        {
            var texture=new Texture2D(wide,high,TextureFormat.RGBA32,false){name=id,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontUnloadUnusedAsset};
            texture.SetPixels32(pixels);texture.Apply(false,!readable);
            var sprite=Sprite.Create(texture,new Rect(0,0,wide,high),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            sprite.name=id;sprite.hideFlags=HideFlags.DontUnloadUnusedAsset;Cache[id]=sprite;return sprite;
        }

        // ---------- fonts ----------

        /// <summary>Body face (Nunito SDF, TMP's default font; its own fallback is Liberation).</summary>
        public static TMP_FontAsset Body => TMP_Settings.defaultFontAsset;

        /// <summary>Display face (Cinzel) for titles, headers, nameplates, banners.</summary>
        public static TMP_FontAsset Display
        {
            get
            {
                if (!_displaySearched)
                {
                    _displaySearched = true;
                    _display = Resources.Load<TMP_FontAsset>("Fonts & Materials/Cinzel SDF");
                }
                return _display != null ? _display : Body;
            }
        }

        // ---------- inline sprite tags ----------

        /// <summary>True when the Lattice TMP sprite sheet is the default sprite asset.</summary>
        public static bool SpriteTagsReady =>
            TMP_Settings.defaultSpriteAsset != null &&
            TMP_Settings.defaultSpriteAsset.name == "LatticeSprites";

        /// <summary>
        /// Rich-text tag for an inline icon, or the fallback string when the sheet
        /// is unavailable (mirrors the old MetaGlyphs graceful degradation).
        /// </summary>
        public static string Tag(string sprite, string fallback = "")
        {
            return SpriteTagsReady ? $"<sprite name=\"{sprite}\">" : fallback;
        }

        /// <summary>Editor/tests teardown safety — drop cached sprite references.</summary>
        public static void ClearCache()
        {
            Cache.Clear();
            _display = null;
            _displaySearched = false;
        }
    }
}
