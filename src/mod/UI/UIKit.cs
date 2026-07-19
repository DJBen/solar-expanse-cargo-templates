using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolarExpanseCargoTemplates.UI
{
    /// <summary>Tiny programmatic uGUI builders shared by the dropdown and the manager window.</summary>
    internal static class UIKit
    {
        // Palette matches solar-expanse-launch-windows (dropdown panels / items / hover).
        public static readonly Color PanelBg = new Color(0.10f, 0.11f, 0.13f, 0.98f);
        public static readonly Color BtnBg = new Color(0.12f, 0.14f, 0.17f, 0.9f);
        public static readonly Color BtnHover = new Color(0.20f, 0.24f, 0.30f, 1f);
        public static readonly Color Accent = new Color(0.10f, 0.22f, 0.14f, 0.9f);
        public static readonly Color Danger = new Color(0.30f, 0.10f, 0.10f, 0.9f);
        public static readonly Color InputBg = new Color(0.05f, 0.06f, 0.07f, 0.95f);

        static TMP_FontAsset headerFont;
        static bool headerFontSearched;

        /// <summary>Oxanium — the game's heading font — with graceful fallback (same as launch-windows).</summary>
        public static TMP_FontAsset HeaderFont(TMP_FontAsset fallback)
        {
            if (!headerFontSearched)
            {
                headerFontSearched = true;
                foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                {
                    var n = f.name ?? "";
                    if (n.IndexOf("Oxanium", StringComparison.OrdinalIgnoreCase) >= 0) { headerFont = f; break; }
                }
            }
            return headerFont != null ? headerFont : fallback;
        }

        public static GameObject MakeVPanel(string name, Transform parent, float width, bool fitHeight = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = PanelBg;
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(9, 9, 5, 5);
            vlg.spacing = 2f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            if (fitHeight)
                go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(width, 0f);
            return go;
        }

        public static GameObject MakeRow(Transform parent, float spacing = 4f)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            return go;
        }

        public static TextMeshProUGUI MakeLabel(Transform parent, TMP_FontAsset font, string text,
                                                 float fontSize = 16f, bool muted = false,
                                                 bool expandWidth = false, float fixedWidth = 0f)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = muted ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 26f;
            if (expandWidth) le.flexibleWidth = 1f;
            else if (fixedWidth > 0f) { le.preferredWidth = fixedWidth; le.minWidth = fixedWidth; }
            return tmp;
        }

        public static Button MakeButton(Transform parent, TMP_FontAsset font, string text, Action onClick,
                                        out TextMeshProUGUI label,
                                        bool expandWidth = false, float fixedWidth = 0f, float height = 30f,
                                        Color? bgColor = null, bool alignLeft = false)
        {
            var go = new GameObject("Btn", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor ?? BtnBg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = BtnHover;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            if (expandWidth) le.flexibleWidth = 1f;
            else if (fixedWidth > 0f) { le.preferredWidth = fixedWidth; le.minWidth = fixedWidth; }

            var lblGO = new GameObject("L", typeof(RectTransform));
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = (RectTransform)lblGO.transform;
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(8f, 0f);
            lblRT.offsetMax = new Vector2(-8f, 0f);
            label = lblGO.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = 15f;
            label.color = Color.white;
            label.alignment = alignLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return btn;
        }

        public static Button MakeButton(Transform parent, TMP_FontAsset font, string text, Action onClick,
                                        bool expandWidth = false, float fixedWidth = 0f, float height = 30f,
                                        Color? bgColor = null, bool alignLeft = false)
            => MakeButton(parent, font, text, onClick, out _, expandWidth, fixedWidth, height, bgColor, alignLeft);

        public static TMP_InputField MakeInput(Transform parent, TMP_FontAsset font, string initial,
                                               TMP_InputField.ContentType contentType, float width,
                                               Action<string> onEndEdit, float height = 30f, bool expandWidth = false)
        {
            var go = new GameObject("Input", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = InputBg;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            if (expandWidth) le.flexibleWidth = 1f;
            else { le.preferredWidth = width; le.minWidth = width; }

            var areaGO = new GameObject("Text Area", typeof(RectTransform));
            areaGO.transform.SetParent(go.transform, false);
            var areaRT = (RectTransform)areaGO.transform;
            areaRT.anchorMin = Vector2.zero;
            areaRT.anchorMax = Vector2.one;
            areaRT.offsetMin = new Vector2(6f, 2f);
            areaRT.offsetMax = new Vector2(-6f, -2f);
            areaGO.AddComponent<RectMask2D>();

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(areaGO.transform, false);
            var textRT = (RectTransform)textGO.transform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = 16f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = false;

            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = img;
            input.textViewport = areaRT;
            input.textComponent = tmp;
            input.contentType = contentType;
            input.text = initial;
            if (onEndEdit != null) input.onEndEdit.AddListener(v => onEndEdit(v));
            return input;
        }

        /// <summary>Vertical ScrollRect (mouse-wheel scrollable); returns the content Transform.</summary>
        public static Transform MakeScroll(Transform parent, float height)
        {
            var go = new GameObject("Scroll", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;

            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(go.transform, false);
            var vpRT = (RectTransform)viewportGO.transform;
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();
            viewportGO.AddComponent<Image>().color = Color.clear;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = (RectTransform)contentGO.transform;
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;
            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = go.AddComponent<ScrollRect>();
            scroll.viewport = vpRT;
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25f;
            return contentGO.transform;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
