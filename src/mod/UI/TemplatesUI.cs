using System;
using System.Collections.Generic;
using Game.UI.Windows.Elements.PlanMissionElements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolarExpanseCargoTemplates.UI
{
    /// <summary>
    /// Injects the "TEMPLATES ▾" button next to Add Resources / Add Modules on the Cargo tab
    /// and owns the dropdown panel (list / apply / save / delete).
    /// </summary>
    public static class TemplatesUI
    {
        const string ButtonName = "CargoTemplatesButton";
        const string PanelName = "CargoTemplatesPanel";

        static GameObject panelGO;      // rebuilt lazily; destroyed when window closes
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
                    label.text = "TEMPLATES ▾";
                    // Kill any leftover autosizing bounds tuned for the original string.
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

            panelGO = new GameObject(PanelName, typeof(RectTransform));
            panelGO.transform.SetParent(canvas.transform, false);
            var bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.09f, 0.97f);

            var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            var csf = panelGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(460f, 0f);
            panelRT.pivot = new Vector2(0f, 1f);
            // Position just below the TEMPLATES button (same canvas → world position is safe).
            var btnRT = (RectTransform)buttonTf;
            Vector3[] corners = new Vector3[4];
            btnRT.GetWorldCorners(corners); // 0 = bottom-left
            panelRT.position = corners[0] + new Vector3(0f, -4f * canvas.scaleFactor, 0f);

            var templates = TemplateStore.Load();

            if (templates.Count == 0)
                MakeLabel(panelGO.transform, font, "No templates saved yet.", muted: true);

            foreach (var template in templates)
            {
                var captured = template;
                var row = MakeRow(panelGO.transform);
                MakeButton(row.transform, font, $"{captured.name}  —  {TemplateService.Summarize(captured)}",
                    () =>
                    {
                        TemplateService.Apply(currentTab, captured);
                        HidePanel();
                    }, expandWidth: true, alignLeft: true);
                MakeButton(row.transform, font, "✕",
                    () =>
                    {
                        var list = TemplateStore.Load();
                        list.RemoveAll(x => x.name == captured.name);
                        TemplateStore.Save(list);
                        HidePanel();
                        BuildPanel(); // refresh
                    }, fixedWidth: 30f);
            }

            MakeButton(panelGO.transform, font, "+ SAVE CURRENT CARGO AS TEMPLATE", SaveCurrent,
                expandWidth: true, bgColor: new Color(0.10f, 0.22f, 0.14f, 0.9f));
        }

        static void SaveCurrent()
        {
            var list = TemplateStore.Load();
            string name = NextName(list);
            var template = TemplateService.SaveCurrent(currentTab, name);
            if (template == null)
            {
                Plugin.Log.LogInfo("No resources in cargo to save as a template.");
                return;
            }
            list.Add(template);
            TemplateStore.Save(list);
            HidePanel();
            BuildPanel(); // refresh with the new entry
        }

        static string NextName(List<CargoTemplate> existing)
        {
            int n = existing.Count + 1;
            string Candidate(int i) => $"Template {i}";
            while (existing.Exists(t => t.name == Candidate(n))) n++;
            return Candidate(n);
        }

        // ---- tiny uGUI builders (same approach as solar-expanse-launch-windows) ----

        static GameObject MakeRow(Transform parent)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            return go;
        }

        static void MakeLabel(Transform parent, TMP_FontAsset font, string text, bool muted = false)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = 14f;
            tmp.color = muted ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 26f;
        }

        static Button MakeButton(Transform parent, TMP_FontAsset font, string text, Action onClick,
                                 bool expandWidth = false, float fixedWidth = 0f,
                                 Color? bgColor = null, bool alignLeft = false)
        {
            var go = new GameObject("Btn", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor ?? new Color(0.12f, 0.14f, 0.18f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.20f, 0.35f, 0.45f, 0.9f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 30f;
            le.preferredHeight = 30f;
            if (expandWidth) le.flexibleWidth = 1f;
            else if (fixedWidth > 0f) { le.preferredWidth = fixedWidth; le.minWidth = fixedWidth; }

            var lblGO = new GameObject("L", typeof(RectTransform));
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = (RectTransform)lblGO.transform;
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(8f, 0f);
            lblRT.offsetMax = new Vector2(-8f, 0f);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = 14f;
            tmp.color = Color.white;
            tmp.alignment = alignLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return btn;
        }
    }
}
