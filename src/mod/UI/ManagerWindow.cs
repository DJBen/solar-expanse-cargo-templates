using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    /// <summary>
    /// Places the top-bar button once (left of the other mod buttons, waiting for them to settle),
    /// then makes it independently draggable — same interaction model as launch-windows' LWMover.
    /// Owns the editor panel (left-aligned under the button, clamped to the screen).
    /// </summary>
    internal class CTMover : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const float PanelWidth = 560f;

        internal RectTransform ShowBtnRT;
        internal TMP_FontAsset Font;

        RectTransform _rt;
        Canvas _canvas;
        RectTransform _canvasRT;

        bool _placed;
        float _spawnTime;
        Vector2 _lastCanvasSize;
        Vector2 _normalizedPos;
        bool _normalizedPosSet;
        Vector2 _dragStartAnchored;
        Vector2 _dragStartScreen;

        GameObject _panelGO;
        Transform _scrollContent;
        int _pickingForIndex = -1; // template index whose "+ ADD RESOURCE" opened the picker

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _canvasRT = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
            _spawnTime = Time.unscaledTime;
        }

        void LateUpdate()
        {
            if (_rt == null || _canvasRT == null || _canvas == null) return;

            if (!_placed)
            {
                // Wait for the other mod buttons to leave their -9999 staging position; after 3s
                // give up on them and anchor to the notification button instead.
                RectTransform refRT = FindReferenceButton();
                if (refRT == null && Time.unscaledTime - _spawnTime > 3f) refRT = ShowBtnRT;
                if (refRT == null) return;
                PlaceLeftOf(refRT);
                _placed = true;
                StoreNormalizedPos();
                _lastCanvasSize = _canvasRT.rect.size;
                return;
            }

            // Keep position stable across resolution changes; otherwise the button stays where
            // the user dragged it — it does NOT follow the other buttons around.
            Vector2 sz = _canvasRT.rect.size;
            if (sz != _lastCanvasSize)
            {
                _lastCanvasSize = sz;
                RestoreFromNormalizedPos();
                PositionPanel();
            }
        }

        void PlaceLeftOf(RectTransform refRT)
        {
            Camera cam = Cam();
            var corners = new Vector3[4];
            refRT.GetWorldCorners(corners); // 1 = top-left
            Vector2 topLeft;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRT, new Vector2(corners[1].x, corners[1].y), cam, out topLeft)) return;
            _rt.anchoredPosition = new Vector2(topLeft.x - 6f - _rt.sizeDelta.x, topLeft.y);
            Clamp();
        }

        Camera Cam() => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        /// <summary>Leftmost settled mod button (ignores ones still parked offscreen).</summary>
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
                    var pos = rt.anchoredPosition;
                    if (pos.x < -4000f || pos.y < -4000f) continue;
                    if (best == null || pos.x < bestX) { best = rt; bestX = pos.x; }
                }
            }
            return best;
        }

        // ── Drag (independent of the other mod buttons) ─────────────────────────────────────

        public void OnBeginDrag(PointerEventData e)
        {
            _dragStartAnchored = _rt.anchoredPosition;
            _dragStartScreen = e.position;
        }

        public void OnDrag(PointerEventData e)
        {
            Camera cam = Cam();
            Vector2 cur, start;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, e.position, cam, out cur)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, _dragStartScreen, cam, out start)) return;
            _rt.anchoredPosition = _dragStartAnchored + (cur - start);
            Clamp();
            PositionPanel();
        }

        public void OnEndDrag(PointerEventData e) => StoreNormalizedPos();

        void Clamp()
        {
            Rect cr = _canvasRT.rect;
            Vector2 s = _rt.sizeDelta, p = _rt.anchoredPosition;
            p.x = Mathf.Clamp(p.x, cr.xMin, cr.xMax - s.x);
            p.y = Mathf.Clamp(p.y, cr.yMin + s.y, cr.yMax);
            _rt.anchoredPosition = p;
        }

        void StoreNormalizedPos()
        {
            Rect cr = _canvasRT.rect;
            if (cr.xMax <= 0f || cr.yMax <= 0f) return;
            _normalizedPos = new Vector2(_rt.anchoredPosition.x / cr.xMax, _rt.anchoredPosition.y / cr.yMax);
            _normalizedPosSet = true;
        }

        void RestoreFromNormalizedPos()
        {
            if (!_normalizedPosSet) return;
            Rect cr = _canvasRT.rect;
            _rt.anchoredPosition = new Vector2(_normalizedPos.x * cr.xMax, _normalizedPos.y * cr.yMax);
            Clamp();
        }

        // ── Panel ───────────────────────────────────────────────────────────────────────────

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

        /// <summary>Left-aligned under the button (clamped so it never leaves the screen).</summary>
        void PositionPanel()
        {
            if (_panelGO == null || _rt == null) return;
            var corners = new Vector3[4];
            _rt.GetWorldCorners(corners); // 0 = bottom-left
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRT, new Vector2(corners[0].x, corners[0].y), Cam(), out local)) return;
            local.y -= 4f;
            Rect cr = _canvasRT.rect;
            local.x = Mathf.Clamp(local.x, cr.xMin, cr.xMax - PanelWidth);
            ((RectTransform)_panelGO.transform).anchoredPosition = local;
        }

        void BuildPanel()
        {
            if (_canvas == null || _rt == null) return;

            _panelGO = UIKit.MakeVPanel("modCargoTemplatesPanel", _canvas.transform, PanelWidth, fitHeight: true);
            _panelGO.AddComponent<LayoutElement>().ignoreLayout = true;
            var panelRT = (RectTransform)_panelGO.transform;
            panelRT.pivot = new Vector2(0f, 1f); // left-aligned dropdown, same as launch-windows

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
