using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Lattice.UI
{
    /// <summary>
    /// POLISH-02: the UI skin — resource-backed access to the 9-slice kit sprites
    /// (Resources/UI/Kit), the standalone icons (Resources/UI/Icons), the licensed
    /// font pair (Cinzel display / Nunito body, both SIL OFL — see CREDITS.md) and
    /// the TMP inline-sprite tags. Every getter degrades gracefully: with the art
    /// missing the whole UI still runs on flat colors and the default font, so
    /// nothing here can wedge a test or a smoke.
    /// </summary>
    public static class UiSkin
    {
        static readonly Dictionary<string, Sprite> Cache = new();
        static TMP_FontAsset _display;
        static bool _displaySearched;

        // ---------- sprites ----------

        public static Sprite Kit(string name) => Load("UI/Kit/", name);
        public static Sprite Icon(string name) => Load("UI/Icons/", name);
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
