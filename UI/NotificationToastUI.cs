using System;
using System.Collections.Generic;
using ItemRoulette.Core;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace ItemRoulette.UI
{
    /// <summary>
    /// Renders an in-game stack of achievement-style notification cards in the bottom-left corner of the screen.
    /// Each card smoothly slides in, displays rich text and badges, and fades out when completed.
    /// </summary>
    public class NotificationToastUI : MonoBehaviour
    {
        public static NotificationToastUI? Instance { get; private set; }

        private GUIStyle? _badgeStyle;
        private GUIStyle? _contentStyle;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Advance animation and timers on active toasts
            NotificationManager.UpdateActiveToasts(Time.unscaledDeltaTime);
        }

        private void InitStyles()
        {
            if (_badgeStyle != null) return;

            _badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = new Color(0.65f, 0.72f, 0.82f, 0.90f) }
            };

            _contentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                richText = true,
                normal = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            try
            {
                if (Run.instance == null) return;

                // Hide if paused or HUD disabled
                if (HUD.readOnlyInstanceList == null || HUD.readOnlyInstanceList.Count == 0) return;
                var hud = HUD.readOnlyInstanceList[0];
                if (hud == null || !hud.gameObject.activeInHierarchy) return;

                InitStyles();

                var toasts = NotificationManager.GetActiveToastsSnapshot();
                if (toasts.Count == 0) return;

                float startX = 24f;
                // Position above character portrait / health area in bottom-left
                float baseY = Screen.height - 180f;

                using (GUIHelper.UseScale(GUIHelper.SafeToastsScale, new Vector2(startX, baseY)))
                {
                    float cardWidth = Mathf.Clamp(Screen.width * 0.28f, 380f, 480f);
                    float spacing = 6f;
                    float currentY = baseY;

                    for (int i = 0; i < toasts.Count; i++)
                    {
                        var toast = toasts[i];
                        float alpha = toast.Alpha;
                        if (alpha <= 0.01f) continue;

                        // Calculate height dynamically
                        string msg = toast.Entry.Message;
                        float textH = _contentStyle!.CalcHeight(new GUIContent(msg), cardWidth - 28f);
                        float cardH = Mathf.Max(38f, 18f + textH + 4f);

                        currentY -= cardH;
                        Rect cardRect = new Rect(startX + toast.SlideOffset, currentY, cardWidth, cardH);
                        currentY -= spacing;

                        // 1. Dark Glass Background
                        Color bg = new Color(0.05f, 0.07f, 0.11f, 0.94f * alpha);
                        GUIHelper.DrawRect(cardRect, bg);

                        // 2. Left Accent Stripe (thickness 4px)
                        Color stripeCol = new Color(toast.Entry.AccentColor.r, toast.Entry.AccentColor.g, toast.Entry.AccentColor.b, 0.98f * alpha);
                        Rect stripeRect = new Rect(cardRect.x, cardRect.y, 4f, cardRect.height);
                        GUIHelper.DrawRect(stripeRect, stripeCol);

                        // 3. Subtle Outer Border
                        Color borderCol = new Color(toast.Entry.AccentColor.r * 0.7f, toast.Entry.AccentColor.g * 0.7f, toast.Entry.AccentColor.b * 0.7f, 0.35f * alpha);
                        GUIHelper.DrawBorder(cardRect, borderCol, 1);

                        // 4. Header Badge: [Time | Stage | Initiator] + Category Icon
                        string icon = GetCategoryIcon(toast.Entry.Category);
                        string categoryName = GetCategoryNameSafe(toast.Entry.Category);
                        string serverName = Localization.Get("ITEMROULETTE_INITIATOR_SERVER");
                        string initName = !string.IsNullOrEmpty(toast.Entry.Initiator) ? toast.Entry.Initiator : serverName;
                        string initColor = string.Equals(initName, serverName, StringComparison.OrdinalIgnoreCase) ? "#94A3B8" : "#38BDF8";
                        string badgeText = $"<color=#{ColorUtility.ToHtmlStringRGB(toast.Entry.AccentColor)}>{icon} {categoryName}</color>  <color=#64748B>•</color>  <color=#94A3B8>[{toast.Entry.TimeFormatted} | {toast.Entry.StageFormatted} | <color={initColor}>{initName}</color>]</color>";

                        Rect badgeRect = new Rect(cardRect.x + 12f, cardRect.y + 4f, cardWidth - 20f, 15f);
                        GUI.Label(badgeRect, badgeText, _badgeStyle!);

                        // 5. Message Content
                        Rect msgRect = new Rect(cardRect.x + 12f, cardRect.y + 19f, cardWidth - 20f, cardH - 22f);
                        GUI.Label(msgRect, msg, _contentStyle!);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in NotificationToastUI.OnGUI: {ex}");
            }
        }

        public static string GetCategoryIcon(NotificationCategory category)
        {
            switch (category)
            {
                case NotificationCategory.Roulette: return "🎰";
                case NotificationCategory.Casino: return "★";
                case NotificationCategory.Chaos: return "🎲";
                case NotificationCategory.Reward: return "📦";
                case NotificationCategory.Curse: return "☠";
                default: return "ℹ";
            }
        }

        public static string GetCategoryNameSafe(NotificationCategory category)
        {
            switch (category)
            {
                case NotificationCategory.Roulette: return Localization.Get("ITEMROULETTE_CAT_ROULETTE");
                case NotificationCategory.Casino: return Localization.Get("ITEMROULETTE_CAT_CASINO");
                case NotificationCategory.Chaos: return Localization.Get("ITEMROULETTE_CAT_CHAOS");
                case NotificationCategory.Reward: return Localization.Get("ITEMROULETTE_CAT_REWARD");
                case NotificationCategory.Curse: return Localization.Get("ITEMROULETTE_CAT_CALAMITY");
                default: return Localization.Get("ITEMROULETTE_CAT_EVENT");
            }
        }
    }
}
