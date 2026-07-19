using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolarExpanseCargoTemplates.UI
{
    /// <summary>
    /// Standalone template editor: a "CARGO TEMPLATES" button in the top bar (next to the other
    /// mod buttons / notification history) opening a window to create, edit and delete templates.
    /// </summary>
    internal static class ManagerWindow
    {
        static readonly FieldInfo FieldShowBtn =
            typeof(NotificationManager).GetField("showNotificationHistory",
                BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo FieldHistoryGO =
            typeof(NotificationManager).GetField("notificationHistory",
                BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Inject(NotificationManager nm)
        {
            try
            {
                Button showBtn = FieldShowBtn?.GetValue(nm) as Button;
                GameObject historyGO = FieldHistoryGO?.GetValue(nm) as GameObject;
                if (showBtn == null || historyGO == null)
                {
                    Plugin.Log.LogError("[CT] notification bar fields not found");
                    return;
                }
                Canvas canvas = showBtn.GetComponentInParent<Canvas>();
                if (canvas == null) { Plugin.Log.LogError("[CT] Canvas not found"); return; }

                TMP_FontAsset font = historyGO.GetComponentInChildren<TextMeshProUGUI>(true)?.font;

                // ── Top-bar button ────────────────────────────────────────────────────────────
                var btnGO = new GameObject("modCargoTemplatesButton", typeof(RectTransform));
                btnGO.transform.SetParent(canvas.transform, false);
                btnGO.transform.SetAsLastSibling();
                btnGO.AddComponent<LayoutElement>().ignoreLayout = true;

                var btnRT = btnGO.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.5f, 0.5f);
                btnRT.anchorMax = new Vector2(0.5f, 0.5f);
                btnRT.pivot = new Vector2(0f, 1f);
                btnRT.sizeDelta = new Vector2(190f, 33f);
                btnRT.anchoredPosition = new Vector2(-9999f, -9999f);

                var bg = btnGO.AddComponent<Image>();
                var srcImg = showBtn.GetComponent<Image>();
                if (srcImg != null)
                { bg.sprite = srcImg.sprite; bg.type = srcImg.type; bg.color = srcImg.color; bg.material = srcImg.material; }
                else bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
                bg.raycastTarget = true;

                var lblGO = new GameObject("L", typeof(RectTransform));
                lblGO.transform.SetParent(btnGO.transform, false);
                var lblRT = (RectTransform)lblGO.transform;
                lblRT.anchorMin = Vector2.zero;
                lblRT.anchorMax = Vector2.one;
                lblRT.offsetMin = Vector2.zero;
                lblRT.offsetMax = Vector2.zero;
                var lbl = lblGO.AddComponent<TextMeshProUGUI>();
                if (font != null) lbl.font = font;
                lbl.text = "CARGO TEMPLATES";
                lbl.fontSize = 15f;
                lbl.color = Color.white;
                lbl.alignment = TextAlignmentOptions.Center;

                var button = btnGO.AddComponent<Button>();
                button.targetGraphic = bg;

                var mover = btnGO.AddComponent<CTMover>();
                mover.ShowBtnRT = showBtn.GetComponent<RectTransform>();
                mover.Font = font;
                button.onClick.AddListener(mover.TogglePanel);

                Plugin.Log.LogInfo("[CT] top-bar button injected");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[CT] Inject exception: {e}");
            }
        }
    }

    /// <summary>Positions the top-bar button (left of other mod buttons) and owns the editor panel.</summary>
    internal class CTMover : MonoBehaviour
    {
        internal RectTransform ShowBtnRT;
        internal TMP_FontAsset Font;

        RectTransform _rt;
        Canvas _canvas;
        RectTransform _canvasRT;
        RectTransform _refRT;
        float _nextRefScan;

        GameObject _panelGO;
        Transform _scrollContent;
        int _pickingForIndex = -1; // template index whose "+ ADD RESOURCE" opened the picker

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _canvasRT = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
        }

        IEnumerator Start()
        {
            yield return null;
            yield return null;
            LateUpdate();
        }

        // The other mod buttons (launch windows / life support / …) settle their positions a few
        // frames after Awake — and this plugin can load BEFORE them. So: rescan for the leftmost
        // reference button once a second, and follow it every frame (GetWorldCorners is cheap).
        void LateUpdate()
        {
            if (_rt == null || _canvasRT == null || _canvas == null) return;
            if (Time.unscaledTime >= _nextRefScan || _refRT == null)
            {
                _nextRefScan = Time.unscaledTime + 1f;
                _refRT = FindReferenceButton() ?? ShowBtnRT;
            }
            if (_refRT == null) return;

            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            var corners = new Vector3[4];
            _refRT.GetWorldCorners(corners); // 1 = top-left
            Vector2 topLeft;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRT, new Vector2(corners[1].x, corners[1].y), cam, out topLeft)) return;
            _rt.anchoredPosition = new Vector2(topLeft.x - 6f - _rt.sizeDelta.x, topLeft.y);
            PositionPanel();
        }

        void PositionPanel()
        {
            if (_panelGO == null || _rt == null) return;
            var corners = new Vector3[4];
            _rt.GetWorldCorners(corners); // 3 = bottom-right
            _panelGO.transform.position = corners[3];
            ((RectTransform)_panelGO.transform).anchoredPosition += new Vector2(0f, -4f);
        }

        /// <summary>Chain to the left of whichever known mod button is present (leftmost wins).</summary>
        RectTransform FindReferenceButton()
        {
            if (_canvas == null) return null;
            RectTransform best = null;
            float bestX = float.MaxValue;
            foreach (RectTransform rt in _canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == _rt) continue;
                string n = rt.gameObject.name ?? "";
                if ((n.Equals("modLaunchWindowsButton", StringComparison.OrdinalIgnoreCase) ||
                     n.Equals("modPowerTrackerButton", StringComparison.OrdinalIgnoreCase) ||
                     n.Equals("modLifeSupportButton", StringComparison.OrdinalIgnoreCase) ||
                     n.Equals("modFleetTrackerButton", StringComparison.OrdinalIgnoreCase)) &&
                    rt.GetComponent<Image>() != null)
                {
                    // Ignore buttons still parked at their offscreen staging position (-9999).
                    var pos = rt.anchoredPosition;
                    if (pos.x < -4000f || pos.y < -4000f) continue;
                    if (best == null || pos.x < bestX) { best = rt; bestX = pos.x; }
                }
            }
            return best;
        }

        internal void TogglePanel()
        {
            if (_panelGO != null) { ClosePanel(); return; }
            BuildPanel();
        }

        internal void ClosePanel()
        {
            if (_panelGO != null) { Destroy(_panelGO); _panelGO = null; }
            _scrollContent = null;
            _pickingForIndex = -1;
        }

        void BuildPanel()
        {
            if (_canvas == null || _rt == null) return;

            _panelGO = UIKit.MakeVPanel("modCargoTemplatesPanel", _canvas.transform, 560f, fitHeight: true);
            _panelGO.AddComponent<LayoutElement>().ignoreLayout = true;
            var panelRT = (RectTransform)_panelGO.transform;
            panelRT.pivot = new Vector2(1f, 1f); // grow down-left: the button sits near the right edge

            // Header
            var header = UIKit.MakeRow(_panelGO.transform);
            var title = UIKit.MakeLabel(header.transform, UIKit.HeaderFont(Font), "CARGO TEMPLATES", 15f,
                muted: false, expandWidth: true);
            title.fontStyle = TMPro.FontStyles.Bold;
            UIKit.MakeButton(header.transform, Font, "+ NEW", () =>
            {
                var list = TemplateStore.Load();
                list.Add(new CargoTemplate { name = NextName(list) });
                TemplateStore.Save(list);
                RebuildContent();
            }, fixedWidth: 80f, bgColor: UIKit.Accent);
            UIKit.MakeButton(header.transform, Font, "×", ClosePanel, fixedWidth: 30f);

            _scrollContent = UIKit.MakeScroll(_panelGO.transform, 430f);
            RebuildContent();

            // Place under the button, right-aligned to it.
            PositionPanel();
        }

        static string NextName(List<CargoTemplate> existing)
        {
            int n = existing.Count + 1;
            string Candidate(int i) => $"Template {i}";
            while (existing.Exists(t => t.name == Candidate(n))) n++;
            return Candidate(n);
        }

        void RebuildContent()
        {
            if (_scrollContent == null) return;
            UIKit.ClearChildren(_scrollContent);

            if (_pickingForIndex >= 0) { BuildResourcePicker(); return; }

            var templates = TemplateStore.Load();
            if (templates.Count == 0)
                UIKit.MakeLabel(_scrollContent, Font, "No templates yet — click + NEW to create one.", muted: true);

            for (int i = 0; i < templates.Count; i++)
            {
                int templateIndex = i;
                var t = templates[i];

                // Template header: [name input][✕]
                var head = UIKit.MakeRow(_scrollContent);
                UIKit.MakeInput(head.transform, Font, t.name, TMP_InputField.ContentType.Standard, 0f,
                    v =>
                    {
                        var list = TemplateStore.Load();
                        if (templateIndex < list.Count && !string.IsNullOrEmpty(v))
                        { list[templateIndex].name = v; TemplateStore.Save(list); }
                    }, expandWidth: true);
                UIKit.MakeButton(head.transform, Font, "✕", () =>
                {
                    var list = TemplateStore.Load();
                    if (templateIndex < list.Count) { list.RemoveAt(templateIndex); TemplateStore.Save(list); }
                    RebuildContent();
                }, fixedWidth: 30f, bgColor: UIKit.Danger);

                // Item rows: [resource][mass input][✕]
                for (int j = 0; j < t.items.Count; j++)
                {
                    int itemIndex = j;
                    var item = t.items[j];
                    var row = UIKit.MakeRow(_scrollContent);
                    UIKit.MakeLabel(row.transform, Font, "    " + TemplateService.ResourceName(item.id),
                        expandWidth: true);
                    UIKit.MakeInput(row.transform, Font,
                        item.mass.ToString("0.##", CultureInfo.InvariantCulture),
                        TMP_InputField.ContentType.DecimalNumber, 90f,
                        v =>
                        {
                            if (!double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double mass))
                                return;
                            var list = TemplateStore.Load();
                            if (templateIndex < list.Count && itemIndex < list[templateIndex].items.Count)
                            { list[templateIndex].items[itemIndex].mass = Math.Max(0, mass); TemplateStore.Save(list); }
                        });
                    UIKit.MakeLabel(row.transform, Font, "t", fixedWidth: 14f, muted: true);
                    UIKit.MakeButton(row.transform, Font, "✕", () =>
                    {
                        var list = TemplateStore.Load();
                        if (templateIndex < list.Count && itemIndex < list[templateIndex].items.Count)
                        { list[templateIndex].items.RemoveAt(itemIndex); TemplateStore.Save(list); }
                        RebuildContent();
                    }, fixedWidth: 30f);
                }

                UIKit.MakeButton(_scrollContent, Font, "+ ADD RESOURCE", () =>
                {
                    _pickingForIndex = templateIndex;
                    RebuildContent();
                }, expandWidth: true, height: 26f, bgColor: new Color(0.10f, 0.14f, 0.20f, 0.9f));

                // Spacer between templates
                UIKit.MakeLabel(_scrollContent, Font, "", 6f).GetComponent<LayoutElement>().minHeight = 8f;
            }
        }

        void BuildResourcePicker()
        {
            UIKit.MakeButton(_scrollContent, Font, "← BACK", () =>
            {
                _pickingForIndex = -1;
                RebuildContent();
            }, expandWidth: true, bgColor: UIKit.BtnBg);

            foreach (var rd in TemplateService.AllCargoResources())
            {
                if (rd == null) continue;
                string id = rd.ID;
                UIKit.MakeButton(_scrollContent, Font, TemplateService.ResourceName(id), () =>
                {
                    var list = TemplateStore.Load();
                    if (_pickingForIndex >= 0 && _pickingForIndex < list.Count)
                    {
                        list[_pickingForIndex].items.Add(new TemplateItem { id = id, mass = 100 });
                        TemplateStore.Save(list);
                    }
                    _pickingForIndex = -1;
                    RebuildContent();
                }, expandWidth: true, height: 26f, alignLeft: true);
            }
        }
    }
}
