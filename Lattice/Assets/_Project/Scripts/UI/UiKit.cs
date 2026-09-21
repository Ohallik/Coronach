using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lattice.UI
{
    /// <summary>
    /// Runtime UI construction helpers — every screen in the game is built in code
    /// through these (project rule: no throwaway serialized layouts), so the POLISH-02
    /// skin (9-slice kit sprites + Cinzel/Nunito type, see UiSkin) propagates everywhere
    /// from here. All helpers degrade to the old flat-color look if the art is missing.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color PanelColor = new(0.06f, 0.09f, 0.14f, 0.92f);
        public static readonly Color TextColor = new(0.88f, 0.93f, 1f);
        public static readonly Color DimTextColor = new(0.55f, 0.63f, 0.74f);
        public static readonly Color GoldColor = new(1f, 0.85f, 0.45f);
        public static readonly Color FrostColor = new(0.69f, 0.8f, 0.92f);
        public static readonly Color ButtonColor = new(0.13f, 0.19f, 0.28f, 0.95f);
        public static readonly Color ButtonHighlight = new(0.24f, 0.38f, 0.55f, 1f);

        public static TMP_FontAsset Font => TMP_Settings.defaultFontAsset;

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
                go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            if (size != Vector2.zero)
                rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Flat color rectangle (dims, bars, tint fills).</summary>
        public static Image Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>9-slice kit sprite. Falls back to a flat panel of the given tint.</summary>
        public static Image Nine(Transform parent, string name, string kitSprite, Color? fallback = null)
        {
            var image = Panel(parent, name, Color.white);
            var sprite = UiSkin.Kit(kitSprite);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                image.color = fallback ?? PanelColor;
            }
            return image;
        }

        /// <summary>The standard framed window panel (chamfered dusk-slate frame).</summary>
        public static Image Frame(Transform parent, string name = "Panel") =>
            Nine(parent, name, "panel_frame");

        /// <summary>The more opaque frame for text-heavy panels (dialogue, message box).</summary>
        public static Image DarkFrame(Transform parent, string name = "Panel") =>
            Nine(parent, name, "panel_dark");

        /// <summary>Full-screen dim + soft vignette (menus over the world).</summary>
        public static Image Dim(Transform parent, float strength = 0.55f)
        {
            var dim = Panel(parent, "Dim", new Color(0f, 0f, 0f, strength));
            Rect(dim.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.raycastTarget = true;
            var sprite = UiSkin.Kit("vignette");
            if (sprite != null)
            {
                var vig = Panel(dim.transform, "Vignette", Color.white);
                vig.sprite = sprite;
                vig.raycastTarget = false;
                Rect(vig.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            return dim;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size,
            Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>Display-face text (Cinzel): titles, headers, nameplates, banners.</summary>
        public static TextMeshProUGUI Heading(Transform parent, string name, string text, float size,
            Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var tmp = Text(parent, name, text, size, color, alignment);
            tmp.font = UiSkin.Display;
            tmp.characterSpacing = 6f;
            return tmp;
        }

        /// <summary>Standalone icon Image (white source art — tint via color).</summary>
        public static Image Icon(Transform parent, string name, string icon, Color color)
        {
            var image = Panel(parent, name, color);
            image.raycastTarget = false;
            var sprite = UiSkin.Icon(icon);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }
            else
            {
                image.color = Color.clear; // no art -> invisible, never a white box
            }
            return image;
        }

        /// <summary>Thin horizontal rule with faded ends.</summary>
        public static Image Divider(Transform parent, string name = "Divider")
        {
            var image = Nine(parent, name, "divider", new Color(1f, 1f, 1f, 0.08f));
            image.raycastTarget = false;
            return image;
        }

        static void ApplySpriteStates(Button button, Image image,
            string normal, string hover, string pressed, string disabled)
        {
            var n = UiSkin.Kit(normal);
            if (n == null)
            {
                // No art: keep the original color-tint behavior.
                var colors = button.colors;
                colors.normalColor = ButtonColor;
                colors.highlightedColor = ButtonHighlight;
                colors.selectedColor = ButtonHighlight;
                colors.pressedColor = ButtonHighlight * 1.2f;
                colors.disabledColor = new Color(0.1f, 0.12f, 0.16f, 0.6f);
                button.colors = colors;
                image.color = Color.white;
                return;
            }
            image.sprite = n;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            button.transition = Selectable.Transition.ColorTint;
            var state=button.colors;state.normalColor=new Color(.72f,.8f,.88f);state.highlightedColor=Color.white;
            state.selectedColor=new Color(1.3f,1.15f,.8f);state.pressedColor=new Color(.6f,1,1);state.disabledColor=new Color(.35f,.35f,.35f,.65f);state.colorMultiplier=1.15f;button.colors=state;
        }

        public static Button Button(Transform parent, string name, string label, Action onClick)
        {
            var image = Panel(parent, name, Color.white);
            var go = image.gameObject;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            ApplySpriteStates(button, image, "button", "button_hover", "button_pressed", "button_disabled");
            HoverSelect.Attach(button); // mouse and pad share one selection model
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            var text = Text(go.transform, "Label", label, 34f, TextColor);
            Rect(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Stretch-anchored: zero the sizeDelta or the label keeps TMP's default
            // 200×50 and overflows the button by 100px per side (left-aligned labels
            // then render outside their rows — Phase-6 screenshot finding).
            ((RectTransform)text.transform).sizeDelta = Vector2.zero;
            return button;
        }

        /// <summary>
        /// List-row button: quieter than Button (slim backing, steel left edge; gold
        /// edge + brighter fill when hovered/selected). The workhorse of every list.
        /// </summary>
        public static Button Row(Transform parent, string name, string label, Action onClick,
            float fontSize = 25f, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var image = Panel(parent, name, Color.white);
            var go = image.gameObject;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            ApplySpriteStates(button, image, "row", "row_hover", "row_hover", "button_disabled");
            HoverSelect.Attach(button); // mouse and pad share one selection model

            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            var text = Text(go.transform, "Label", label, fontSize, TextColor, align);
            Rect(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ((RectTransform)text.transform).sizeDelta = Vector2.zero;
            text.margin = new Vector4(42f, 0f, 42f, 0f);
            return button;
        }

        /// <summary>
        /// POLISH-05: pointer hover moves the EventSystem selection to the hovered
        /// selectable, so mouse and pad share one selection model (the world-map
        /// pins' gold ring + info card follow whatever is selected).
        /// </summary>
        /// <summary>
        /// POLISH-58: a kit-styled drag slider (bar frame + gold fill + keycap-round
        /// handle). Mouse drags it; as a Selectable, pad/keyboard left-right nudges it
        /// natively — the settings pages keep their stepper chevrons beside it anyway.
        /// </summary>
        public static Slider Slider(Transform parent, string name, float value01,
            Action<float> onChanged)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var back = Nine(go.transform, "Back", "bar_frame", new Color(0.1f, 0.14f, 0.2f, 0.9f));
            Rect(back.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ((RectTransform)back.transform).offsetMin = new Vector2(0f, 8f);
            ((RectTransform)back.transform).offsetMax = new Vector2(0f, -8f);
            back.raycastTarget = true;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(6f, 12f);
            fillAreaRect.offsetMax = new Vector2(-6f, -12f);
            var fill = Nine(fillArea.transform, "Fill", "bar_fill", new Color(1f, 0.85f, 0.45f, 0.9f));
            fill.color = GoldColor;
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);
            var handle = Nine(handleArea.transform, "Handle", "keycap", ButtonHighlight);
            var handleRect = handle.rectTransform;
            handleRect.sizeDelta = new Vector2(22f, 26f);

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value01);
            if (onChanged != null)
                slider.onValueChanged.AddListener(v => onChanged(v));
            var colors = slider.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
            colors.selectedColor = new Color(1f, 0.95f, 0.8f);
            slider.colors = colors;
            return slider;
        }

        /// <summary>Wire vertical keyboard/gamepad navigation through a button column.</summary>
        public static void LinkVertical(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var nav = items[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = items[(i - 1 + items.Length) % items.Length];
                nav.selectOnDown = items[(i + 1) % items.Length];
                items[i].navigation = nav;
            }
        }

        /// <summary>
        /// Controller pass: wire a pane of selectables as the GRID it is laid out as.
        /// LinkVertical chained everything in build order: a stepper chevron pair became
        /// "down" from each other, three formation radios stacked into a column that is
        /// drawn as a row, and left/right did nothing anywhere. Items sharing a line
        /// (anchored y within <paramref name="rowTolerance"/>) link left/right in x
        /// order; up/down goes to the nearest-x item on the adjacent line, wrapping
        /// top and bottom. The leftmost item of each line keeps a null left so the caller
        /// can hang a tab column off it. Items must share their anchoring, which every
        /// GameMenu pane item does.
        /// </summary>
        public static void LinkGrid(IList<Selectable> items, float rowTolerance = 26f)
        {
            var placed = items.Where(i => i != null)
                .Select(i => (item: i, pos: ((RectTransform)i.transform).anchoredPosition))
                .OrderByDescending(t => t.pos.y).ThenBy(t => t.pos.x)
                .ToList();
            var lines = new List<List<(Selectable item, Vector2 pos)>>();
            foreach (var t in placed)
            {
                if (lines.Count > 0 && Mathf.Abs(lines[^1][0].pos.y - t.pos.y) <= rowTolerance)
                    lines[^1].Add(t);
                else
                    lines.Add(new List<(Selectable, Vector2)> { t });
            }
            foreach (var line in lines)
                line.Sort((a, b) => a.pos.x.CompareTo(b.pos.x));

            static Selectable Nearest(List<(Selectable item, Vector2 pos)> line, float x)
            {
                Selectable best = null;
                float bestD = float.MaxValue;
                foreach (var (item, pos) in line)
                {
                    float d = Mathf.Abs(pos.x - x);
                    if (d < bestD) { bestD = d; best = item; }
                }
                return best;
            }

            for (int li = 0; li < lines.Count; li++)
            {
                var line = lines[li];
                var above = lines[(li - 1 + lines.Count) % lines.Count];
                var below = lines[(li + 1) % lines.Count];
                for (int k = 0; k < line.Count; k++)
                {
                    var (item, pos) = line[k];
                    var nav = item.navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnLeft = k > 0 ? line[k - 1].item : null;
                    nav.selectOnRight = k < line.Count - 1 ? line[k + 1].item : null;
                    nav.selectOnUp = lines.Count > 1 ? Nearest(above, pos.x) : null;
                    nav.selectOnDown = lines.Count > 1 ? Nearest(below, pos.x) : null;
                    item.navigation = nav;
                }
            }
        }
    }
}
