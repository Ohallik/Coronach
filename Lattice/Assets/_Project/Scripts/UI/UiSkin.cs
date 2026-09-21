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
            if(name=="panel_frame"||name=="panel_dark")return Slice("ui-panel",new(.02f,.025f,.96f,.915f),"panel",new(85,85,85,85),200);
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

        // ---------- fonts ----------

        /// <summary>Body face (Nunito). Falls back to the TMP default (which IS Nunito once wired).</summary>
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
