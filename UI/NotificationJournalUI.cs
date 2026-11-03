using System;
using System.Collections.Generic;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;

namespace ItemRoulette.UI
{
    /// <summary>
    /// Interactive modal journal window (Hotkey [J]) displaying the complete archive of run notifications.
    /// Supports category filtering, smooth scrolling (newest first), dragging, and clean ESC dismissal.
    /// </summary>
    public class NotificationJournalUI : MonoBehaviour
    {
        public static NotificationJournalUI? Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._isOpen;

        private bool _isOpen = false;
        private Rect _windowRect = new Rect(120f, 100f, 740f, 540f);
        private bool _isWindowPosInitialized = false;
        private bool _isDragging = false;
        private Vector2 _dragMouseStart;
        private Vector2 _dragWindowStart;

        private Vector2 _scrollPosition = Vector2.zero;
        private NotificationCategory _activeFilter = NotificationCategory.All;

        private GUIStyle? _winHeaderStyle;
        private GUIStyle? _badgeStyle;
        private GUIStyle? _entryStyle;
        private GUIStyle? _tabBtnStyle;
        private GUIStyle? _emptyHintStyle;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetOpen(bool open)
        {
            if (_isOpen == open) return;
            _isOpen = open;

            var pcmc = PlayerUtil.GetLocalMaster()?.playerCharacterMasterController;
            CasinoRouletteUI.SuppressPlayerButtons(pcmc);

            if (_isOpen)
            {
                UiCommon.AcquireCursor(this);
                EnsureWindowPosition();
                PlaySound("Play_UI_menuClick");
            }
            else
            {
                UiCommon.ReleaseCursor(this);
                _isDragging = false;
                PlaySound("Play_UI_menuClick");
            }
        }

        public void Toggle()
        {
            SetOpen(!_isOpen);
        }

        private void EnsureWindowPosition()
        {
            if (!_isWindowPosInitialized)
            {
                float x = Mathf.Max(20f, (Screen.width - _windowRect.width) / 2f);
                float y = Mathf.Max(30f, (Screen.height - _windowRect.height) / 2f);
                _windowRect.position = new Vector2(x, y);
                _isWindowPosInitialized = true;
            }
        }

        private void Update()
        {
            // Toggle on configured hotkey (default J)
            if (Input.GetKeyDown(ModConfig.NotificationJournalHotkeyKey))
            {
                if (!GUIHelper.IsTypingOrInputBlocked())
                {
                    Toggle();
                }
            }

            // Close on ESC
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                UiCommon.CloseOpenMenus();
            }
        }

        private void InitStyles()
        {
            if (_winHeaderStyle != null) return;

            _winHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = new Color(0.95f, 0.82f, 0.25f) }
            };

            _badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = new Color(0.65f, 0.75f, 0.88f) }
            };

            _entryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                richText = true,
                normal = { textColor = Color.white }
            };

            _tabBtnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = Color.white }
            };

            _emptyHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = new Color(0.60f, 0.65f, 0.75f) }
            };
        }

        private void HandleWindowDrag(Rect dragHandleRect)
        {
            var ev = Event.current;
            Vector2 currentMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            if (ev.type == EventType.MouseDown && ev.button == 0 && dragHandleRect.Contains(ev.mousePosition))
            {
                _isDragging = true;
                _dragMouseStart = currentMouse;
                _dragWindowStart = _windowRect.position;
                ev.Use();
            }
            else if (_isDragging && Input.GetMouseButton(0))
            {
                Vector2 offset = currentMouse - _dragMouseStart;
                float newX = Mathf.Clamp(_dragWindowStart.x + offset.x, 0, Mathf.Max(0, Screen.width - _windowRect.width));
                float newY = Mathf.Clamp(_dragWindowStart.y + offset.y, 0, Mathf.Max(0, Screen.height - _windowRect.height));
                _windowRect.position = new Vector2(newX, newY);
                if (ev.type == EventType.MouseDrag) ev.Use();
            }
            else if (_isDragging && (!Input.GetMouseButton(0) || ev.type == EventType.MouseUp))
            {
                _isDragging = false;
                if (ev.type == EventType.MouseUp) ev.Use();
            }
        }

        private bool DrawUnifiedButton(Rect rect, string text, Color baseBg, Color baseBorder, Color textColor, int fontSize = 11, FontStyle fontStyle = FontStyle.Bold)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Color bg = hover ? Color.Lerp(baseBg, Color.white, 0.15f) : baseBg;
            Color border = hover ? Color.Lerp(baseBorder, Color.white, 0.30f) : baseBorder;

            GUIHelper.DrawRect(rect, bg);
            GUIHelper.DrawBorder(rect, border, 1);

            if (_tabBtnStyle != null)
            {
                _tabBtnStyle.fontSize = fontSize;
                _tabBtnStyle.fontStyle = fontStyle;
                _tabBtnStyle.normal.textColor = textColor;
                GUI.Label(rect, text, _tabBtnStyle);
            }

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void OnGUI()
        {
            try
            {
                if (!_isOpen || Run.instance == null) return;

                InitStyles();
                EnsureWindowPosition();

                // 1. Window Background & Border
                GUIHelper.DrawRect(_windowRect, new Color(0.06f, 0.08f, 0.12f, 0.96f));
                GUIHelper.DrawBorder(_windowRect, new Color(0.35f, 0.70f, 0.95f, 0.90f), 1);

                // 2. Header Bar: Title on Left, Drag & Close Buttons on Right
                Rect titleRect = new Rect(_windowRect.x + 14f, _windowRect.y + 6f, _windowRect.width - 90f, 22f);
                GUI.Label(titleRect, Localization.Get("ITEMROULETTE_JOURNAL_TITLE"), _winHeaderStyle);

                // Drag Button [ ✥ ]
                Rect dragBtnRect = new Rect(_windowRect.xMax - 62f, _windowRect.y + 5f, 26f, 22f);
                HandleWindowDrag(dragBtnRect);
                bool isDragHover = dragBtnRect.Contains(Event.current.mousePosition);
                Color dragBg = _isDragging ? new Color(0.20f, 0.45f, 0.70f, 0.95f) : (isDragHover ? new Color(0.22f, 0.28f, 0.38f, 0.95f) : new Color(0.14f, 0.17f, 0.24f, 0.92f));
                Color dragBorder = _isDragging ? new Color(0.40f, 0.85f, 0.95f, 1f) : (isDragHover ? new Color(0.45f, 0.80f, 0.95f, 0.90f) : new Color(0.38f, 0.45f, 0.58f, 0.80f));
                Color dragText = _isDragging ? Color.white : (isDragHover ? new Color(0.40f, 0.85f, 0.95f, 1f) : new Color(0.55f, 0.75f, 0.88f, 0.90f));
                DrawUnifiedButton(dragBtnRect, "✥", dragBg, dragBorder, dragText, 14, FontStyle.Bold);

                // Close Button [ ✕ ]
                Rect closeBtnRect = new Rect(_windowRect.xMax - 32f, _windowRect.y + 5f, 24f, 22f);
                bool isCloseHover = closeBtnRect.Contains(Event.current.mousePosition);
                Color closeBg = isCloseHover ? new Color(0.70f, 0.18f, 0.22f, 0.95f) : new Color(0.14f, 0.16f, 0.22f, 0.90f);
                Color closeBorder = isCloseHover ? new Color(1f, 0.45f, 0.50f, 1f) : new Color(0.42f, 0.35f, 0.40f, 0.75f);
                Color closeText = isCloseHover ? Color.white : new Color(0.85f, 0.85f, 0.85f);
                if (DrawUnifiedButton(closeBtnRect, "✕", closeBg, closeBorder, closeText, 12, FontStyle.Bold))
                {
                    SetOpen(false);
                }

                // Divider Line
                GUIHelper.DrawRect(new Rect(_windowRect.x + 10f, _windowRect.y + 32f, _windowRect.width - 20f, 1f), new Color(0.25f, 0.28f, 0.38f, 0.80f));

                // 3. Stats Bar
                int stage = Run.instance.stageClearCount + 1;
                float runTime = Run.instance.GetRunStopwatch();
                int m = Mathf.FloorToInt(runTime / 60f);
                int s = Mathf.FloorToInt(runTime % 60f);
                string timeStr = $"{m:D2}:{s:D2}";
                var allEntries = NotificationManager.GetArchiveSnapshot();

                string statsText = Localization.FormatToken("ITEMROULETTE_JOURNAL_STATS", stage, timeStr, allEntries.Count);
                GUI.Label(new Rect(_windowRect.x + 14f, _windowRect.y + 38f, _windowRect.width - 28f, 18f), statsText, _badgeStyle!);

                // 4. Category Filter Tabs (5 tabs: ВСЕ, РУЛЕТКА, КАЗИНО, ХАОС, НАГРАДЫ)
                float tabY = _windowRect.y + 60f;
                float tabH = 24f;
                float tabSpacing = 5f;
                float tabW = (_windowRect.width - 28f - tabSpacing * 4f) / 5f;

                DrawCategoryTab(new Rect(_windowRect.x + 14f, tabY, tabW, tabH), NotificationCategory.All, Localization.Get("ITEMROULETTE_TAB_ALL"), allEntries.Count);
                DrawCategoryTab(new Rect(_windowRect.x + 14f + (tabW + tabSpacing), tabY, tabW, tabH), NotificationCategory.Roulette, Localization.Get("ITEMROULETTE_TAB_ROULETTE"), CountCategory(allEntries, NotificationCategory.Roulette));
                DrawCategoryTab(new Rect(_windowRect.x + 14f + (tabW + tabSpacing) * 2f, tabY, tabW, tabH), NotificationCategory.Casino, Localization.Get("ITEMROULETTE_TAB_CASINO"), CountCategory(allEntries, NotificationCategory.Casino));
                DrawCategoryTab(new Rect(_windowRect.x + 14f + (tabW + tabSpacing) * 3f, tabY, tabW, tabH), NotificationCategory.Chaos, Localization.Get("ITEMROULETTE_TAB_CHAOS"), CountCategory(allEntries, NotificationCategory.Chaos));
                DrawCategoryTab(new Rect(_windowRect.x + 14f + (tabW + tabSpacing) * 4f, tabY, tabW, tabH), NotificationCategory.Reward, Localization.Get("ITEMROULETTE_TAB_REWARDS"), CountCategory(allEntries, NotificationCategory.Reward));

                // 5. Scroll View for Entries
                float scrollY = tabY + tabH + 8f;
                float scrollH = _windowRect.height - 138f;
                Rect scrollRect = new Rect(_windowRect.x + 14f, scrollY, _windowRect.width - 28f, scrollH);
                GUIHelper.DrawRect(scrollRect, new Color(0.04f, 0.05f, 0.08f, 0.85f));
                GUIHelper.DrawBorder(scrollRect, new Color(0.20f, 0.24f, 0.32f, 0.70f), 1);

                // Filter matching entries
                var filtered = new List<NotificationEntry>();
                for (int i = 0; i < allEntries.Count; i++)
                {
                    if (_activeFilter == NotificationCategory.All || 
                        allEntries[i].Category == _activeFilter ||
                        (_activeFilter == NotificationCategory.Chaos && allEntries[i].Category == NotificationCategory.Curse))
                    {
                        filtered.Add(allEntries[i]);
                    }
                }

                if (filtered.Count == 0)
                {
                    GUI.Label(scrollRect, Localization.Get("ITEMROULETTE_JOURNAL_EMPTY"), _emptyHintStyle!);
                }
                else
                {
                    // Calculate content height (display newest first)
                    float cardSpacing = 4f;
                    float viewWidth = scrollRect.width - 20f;
                    float totalContentH = 0f;

                    var cardHeights = new float[filtered.Count];
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        int idx = filtered.Count - 1 - i; // reverse chronological
                        float textH = _entryStyle!.CalcHeight(new GUIContent(filtered[idx].Message), viewWidth - 28f);
                        float cH = Mathf.Max(38f, 18f + textH + 4f);
                        cardHeights[i] = cH;
                        totalContentH += cH + cardSpacing;
                    }

                    Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(scrollH - 4f, totalContentH + 10f));
                    _scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, viewRect);

                    float itemY = 6f;
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        int idx = filtered.Count - 1 - i;
                        var entry = filtered[idx];
                        float cH = cardHeights[i];

                        Rect cRect = new Rect(4f, itemY, viewWidth - 8f, cH);

                        // Card background
                        GUIHelper.DrawRect(cRect, new Color(0.08f, 0.10f, 0.15f, 0.90f));

                        // Left Accent Stripe
                        GUIHelper.DrawRect(new Rect(cRect.x, cRect.y, 4f, cRect.height), entry.AccentColor);

                        // Border
                        GUIHelper.DrawBorder(cRect, new Color(entry.AccentColor.r * 0.6f, entry.AccentColor.g * 0.6f, entry.AccentColor.b * 0.6f, 0.35f), 1);

                        // Time Badge + Category + Initiator
                        string catName = NotificationToastUI.GetCategoryNameSafe(entry.Category);
                        string serverName = Localization.Get("ITEMROULETTE_INITIATOR_SERVER");
                        string initName = !string.IsNullOrEmpty(entry.Initiator) ? entry.Initiator : serverName;
                        string initColor = string.Equals(initName, serverName, StringComparison.OrdinalIgnoreCase) ? "#94A3B8" : "#38BDF8";
                        string badge = $"<color=#{ColorUtility.ToHtmlStringRGB(entry.AccentColor)}>● {catName}</color>  <color=#64748B>•</color>  <color=#94A3B8>[{entry.TimeFormatted} | {entry.StageFormatted} | <color={initColor}>{initName}</color>]</color>";
                        GUI.Label(new Rect(cRect.x + 12f, cRect.y + 4f, cRect.width - 20f, 15f), badge, _badgeStyle!);

                        // Message Text
                        GUI.Label(new Rect(cRect.x + 12f, cRect.y + 19f, cRect.width - 20f, cH - 22f), entry.Message, _entryStyle!);

                        itemY += cH + cardSpacing;
                    }

                    GUI.EndScrollView();
                }

                // 6. Footer Action Bar
                float footY = _windowRect.yMax - 32f;
                float footBtnW = 140f;

                // Clear Archive button
                Rect clearRect = new Rect(_windowRect.x + 14f, footY, footBtnW, 24f);
                if (DrawUnifiedButton(clearRect, Localization.Get("ITEMROULETTE_JOURNAL_CLEAR"), new Color(0.24f, 0.12f, 0.14f, 0.90f), new Color(0.60f, 0.22f, 0.26f, 0.80f), new Color(0.95f, 0.70f, 0.75f), 10, FontStyle.Bold))
                {
                    NotificationManager.ClearArchive();
                    PlaySound("Play_UI_menuClick");
                }

                // Hotkey Hint
                Rect hintRect = new Rect(_windowRect.x + footBtnW + 24f, footY, _windowRect.width - (footBtnW * 2f) - 48f, 24f);
                string hotkeyHint = Localization.FormatToken("ITEMROULETTE_JOURNAL_HINT", ModConfig.NotificationJournalHotkeyKey);
                GUI.Label(hintRect, $"<color=#64748B>{hotkeyHint}</color>", _emptyHintStyle!);

                // Close Button
                Rect closeFooterRect = new Rect(_windowRect.xMax - footBtnW - 14f, footY, footBtnW, 24f);
                if (DrawUnifiedButton(closeFooterRect, Localization.Get("ITEMROULETTE_JOURNAL_CLOSE"), new Color(0.14f, 0.17f, 0.24f, 0.90f), new Color(0.35f, 0.42f, 0.55f, 0.75f), Color.white, 10, FontStyle.Bold))
                {
                    SetOpen(false);
                }

                // Top Cursor
                GUIHelper.DrawTopCursor(_windowRect);

                // Consume unhandled mouse clicks to prevent shooting in background
                var ev = Event.current;
                if (ev != null && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp) && _windowRect.Contains(ev.mousePosition))
                {
                    ev.Use();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in NotificationJournalUI.OnGUI: {ex}");
            }
        }

        private void DrawCategoryTab(Rect rect, NotificationCategory category, string title, int count)
        {
            bool isSelected = (_activeFilter == category);
            Color bg = isSelected ? new Color(0.20f, 0.45f, 0.70f, 0.95f) : new Color(0.12f, 0.15f, 0.22f, 0.90f);
            Color border = isSelected ? new Color(0.40f, 0.85f, 0.95f, 1f) : new Color(0.30f, 0.35f, 0.46f, 0.75f);
            Color textCol = isSelected ? Color.white : new Color(0.85f, 0.88f, 0.95f);
            string display = $"{title} ({count})";

            if (DrawUnifiedButton(rect, display, bg, border, textCol, 11, FontStyle.Bold))
            {
                _activeFilter = category;
                _scrollPosition = Vector2.zero;
                PlaySound("Play_UI_menuClick");
            }
        }

        private static int CountCategory(List<NotificationEntry> list, NotificationCategory cat)
        {
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (cat == NotificationCategory.All) count++;
                else if (cat == NotificationCategory.Chaos && (list[i].Category == NotificationCategory.Chaos || list[i].Category == NotificationCategory.Curse)) count++;
                else if (list[i].Category == cat) count++;
            }
            return count;
        }

        private void PlaySound(string sound)
        {
            try { RoR2.Util.PlaySound(sound, RoR2.RoR2Application.instance.gameObject); } catch { }
        }
    }
}
