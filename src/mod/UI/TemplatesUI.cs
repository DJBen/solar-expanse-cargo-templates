using System;
using System.Collections.Generic;
using Game.UI.Windows.Elements.PlanMissionElements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolarExpanseCargoTemplates.UI
{
    /// <summary>
    /// The Plan Mission → Cargo side: a "TEMPLATES ▾" button whose dropdown only LISTS templates.
    /// Each row: apply button (resources the origin can't cover are red) + multiplier input (default 1).
    /// Templates are created/edited in the standalone CARGO TEMPLATES window (top bar).
    /// </summary>
    public static class TemplatesUI
    {
        const string ButtonName = "CargoTemplatesButton";
        const string PanelName = "CargoTemplatesDropdown";

        static GameObject panelGO;      // rebuilt on open; destroyed when tab deactivates
        static PMTabCargo currentTab;

        public static void EnsureInjected(PMTabCargo tab)
        {
            try
            {
                currentTab = tab;
                RectTransform addCargoRT = tab.ResourcesList != null ? tab.ResourcesList.AddCargoRectTransform : null;
                if (addCargoRT == null) return;

                Transform parent = addCargoRT.parent;
                if (parent == null || parent.Find(ButtonName) != null) return; // already injected

                // Clone the game's own ADD RESOURCES button to inherit styling exactly.
                GameObject clone = UnityEngine.Object.Instantiate(addCargoRT.gameObject, parent);
                clone.name = ButtonName;
                clone.SetActive(true);

                // Strip game scripts (tooltips, localizers, click handlers defined in Assembly-CSharp)
                // so the clone is inert except for the Unity Button itself.
                foreach (var mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    string asm = mb.GetType().Assembly.GetName().Name;
                    if (asm == "Assembly-CSharp" || asm == "Assembly-CSharp-firstpass")
                        UnityEngine.Object.DestroyImmediate(mb);
                }

                Button btn = clone.GetComponent<Button>();
                if (btn == null) btn = clone.AddComponent<Button>();
                btn.onClick = new Button.ButtonClickedEvent(); // drop cloned persistent listeners
                btn.interactable = true;
                btn.onClick.AddListener(TogglePanel);

                var label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    // No dropdown-arrow glyph: the game's font lacks ▾ and renders a tofu box.
                    label.text = "TEMPLATES";
                    label.enableAutoSizing = false;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Template button injection failed: {e}");
            }
        }

        public static void HidePanel()
        {
            if (panelGO != null)
            {
                UnityEngine.Object.Destroy(panelGO);
                panelGO = null;
            }
        }

        static void TogglePanel()
        {
            if (panelGO != null) { HidePanel(); return; }
            BuildPanel();
        }

        static void BuildPanel()
        {
            if (currentTab == null || currentTab.ResourcesList == null) return;
            RectTransform anchorRT = currentTab.ResourcesList.AddCargoRectTransform;
            if (anchorRT == null) return;
            Transform buttonTf = anchorRT.parent != null ? anchorRT.parent.Find(ButtonName) : null;
            if (buttonTf == null) return;

            Canvas canvas = anchorRT.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            TMP_FontAsset font = null;
            var srcLabel = buttonTf.GetComponentInChildren<TextMeshProUGUI>(true);
            if (srcLabel != null) font = srcLabel.font;

            panelGO = UIKit.MakeVPanel(PanelName, canvas.transform, 460f, fitHeight: true);
            var panelRT = (RectTransform)panelGO.transform;
            // Right-aligned: the button sits near the window's right edge, so anchor the panel's
            // top-RIGHT corner to the button's bottom-right and let it grow leftward.
            panelRT.pivot = new Vector2(1f, 1f);
            var btnRT = (RectTransform)buttonTf;
            Vector3[] corners = new Vector3[4];
            btnRT.GetWorldCorners(corners); // 3 = bottom-right
            panelRT.position = corners[3] + new Vector3(0f, -4f * canvas.scaleFactor, 0f);

            var templates = TemplateStore.Load();

            if (templates.Count == 0)
            {
                UIKit.MakeLabel(panelGO.transform, font,
                    "No templates yet.", muted: true);
                UIKit.MakeLabel(panelGO.transform, font,
                    "Configure them in the CARGO TEMPLATES window (top bar).", 12f, muted: true);
                return;
            }

            foreach (var template in templates)
            {
                var captured = template;
                int multiplier = 1;

                var row = UIKit.MakeRow(panelGO.transform);
                row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;

                // ── Apply block: name on the first line, wrapping icon+amount list below ──────
                var blockGO = new GameObject("TplBtn", typeof(RectTransform));
                blockGO.transform.SetParent(row.transform, false);
                var img = blockGO.AddComponent<Image>();
                img.color = UIKit.BtnBg;
                var btn = blockGO.AddComponent<Button>();
                btn.targetGraphic = img;
                var colors = btn.colors;
                colors.highlightedColor = UIKit.BtnHover;
                btn.colors = colors;
                btn.onClick.AddListener(() =>
                {
                    TemplateService.Apply(currentTab, captured, multiplier);
                    HidePanel();
                });
                blockGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
                var blockVlg = blockGO.AddComponent<VerticalLayoutGroup>();
                blockVlg.padding = new RectOffset(8, 8, 4, 6);
                blockVlg.spacing = 2f;
                blockVlg.childForceExpandWidth = true;
                blockVlg.childForceExpandHeight = false;
                blockVlg.childControlWidth = true;
                blockVlg.childControlHeight = true;

                var nameTmp = MakeBlockLabel(blockGO.transform, font, $"<b>{captured.name}</b>", wrap: false);
                nameTmp.raycastTarget = false;
                var detailTmp = MakeBlockLabel(blockGO.transform, font,
                    TemplateService.SummarizeForOrigin(currentTab, captured, 1), wrap: true);
                detailTmp.fontSize = 14f;
                detailTmp.raycastTarget = false;

                // ── Multiplier (top-aligned, on the name line) ────────────────────────────────
                UIKit.MakeLabel(row.transform, font, "×", fixedWidth: 14f, muted: true);
                var multInput = UIKit.MakeInput(row.transform, font, "1",
                    TMP_InputField.ContentType.IntegerNumber, 46f, null);
                // Live update on every keystroke, not just on defocus.
                multInput.onValueChanged.AddListener(v =>
                {
                    multiplier = ParseMultiplier(v);
                    if (detailTmp != null)
                        detailTmp.text = TemplateService.SummarizeForOrigin(currentTab, captured, multiplier);
                });
            }
        }

        /// <summary>Layout-driven TMP line inside the apply block; wrapping lines auto-expand.</summary>
        static TextMeshProUGUI MakeBlockLabel(Transform parent, TMP_FontAsset font, string text, bool wrap)
        {
            var go = new GameObject("Line", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = 16f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = wrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        static int ParseMultiplier(string v)
        {
            if (!int.TryParse(v, out int m) || m < 1) m = 1;
            return m;
        }
    }
}
