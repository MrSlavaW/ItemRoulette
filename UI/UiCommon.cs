using System;
using System.Collections.Generic;
using ItemRoulette.Configuration;
using ItemRoulette.Core;
using RoR2;
using UnityEngine;

namespace ItemRoulette.UI
{
    /// <summary>
    /// Centralized UI primitives, zero-allocation textures, shared styles, cursor token manager, and toast system.
    /// Preserves 100% of existing IMGUI layout, colors, and visual presentation while eliminating GPU texture leaks.
    /// </summary>
    public static class UiCommon
    {
        // =====================================================================
        // 1. Zero-Allocation Drawing Primitives
        // =====================================================================

        public static void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        public static void DrawBorder(Rect rect, Color color, int thickness = 1)
        {
            if (thickness <= 0) return;
            Color prev = GUI.color;
            GUI.color = color;

            // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            // Left
            GUI.DrawTexture(new Rect(rect.x, rect.y + thickness, thickness, rect.height - (thickness * 2)), Texture2D.whiteTexture);
            // Right
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y + thickness, thickness, rect.height - (thickness * 2)), Texture2D.whiteTexture);

            GUI.color = prev;
        }

        public static void DrawCenterNeedle(float centerX, float vpY, float vpH)
        {
            Color gold = new Color(1f, 0.82f, 0.12f, 1f);

            // Thin vertical line
            DrawRect(new Rect(centerX - 1f, vpY, 2f, vpH), gold);

            // Top indicator triangle pointing down
            DrawRect(new Rect(centerX - 4f, vpY, 8f, 2f), gold);
            DrawRect(new Rect(centerX - 2f, vpY + 2f, 4f, 2f), gold);

            // Bottom indicator triangle pointing up
            DrawRect(new Rect(centerX - 2f, vpY + vpH - 4f, 4f, 2f), gold);
            DrawRect(new Rect(centerX - 4f, vpY + vpH - 2f, 8f, 2f), gold);
        }

        public static void DrawProgressBar(Rect barRect, float progress, Color fillColor, Color borderColor, string label, GUIStyle? style)
        {
            // 1. Dark sleek background
            DrawRect(barRect, new Color(0.05f, 0.07f, 0.11f, 0.96f));

            // 2. Smooth Filling Bar
            float fillWidth = Mathf.Max(0f, (barRect.width - 4f) * Mathf.Clamp01(progress));
            Rect fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, fillWidth, barRect.height - 4f);
            DrawRect(fillRect, fillColor);

            // 3. Border
            DrawBorder(barRect, borderColor, 1);

            // 4. Centered label with drop shadow for 100% crisp readability
            if (style != null && !string.IsNullOrEmpty(label))
            {
                Color prevCol = style.normal.textColor;

                Rect shadowRect = new Rect(barRect.x + 1f, barRect.y + 1f, barRect.width, barRect.height);
                style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
                GUI.Label(shadowRect, label, style);

                style.normal.textColor = Color.white;
                GUI.Label(barRect, label, style);

                style.normal.textColor = prevCol;
            }
        }

        // =====================================================================
        // 2. Toast Notification Component
        // =====================================================================

        public class ToastState
        {
            public float Timer { get; private set; } = 0f;
            public string Message { get; private set; } = "";
            public Color Color { get; private set; } = Color.white;

            public void Show(string message, Color color, float duration = 3.5f)
            {
                Message = message;
                Color = color;
                Timer = duration;
            }

            public void Update(float unscaledDeltaTime)
            {
                if (Timer > 0f)
                {
                    Timer -= unscaledDeltaTime;
                }
            }

            public void Draw(Rect anchorRect, float yOffset, GUIStyle style, float maxToastWidth = 560f)
            {
                if (Timer <= 0f || string.IsNullOrEmpty(Message)) return;

                float alpha = Mathf.Clamp01(Timer / 0.5f);
                float toastWidth = Mathf.Min(anchorRect.width, maxToastWidth);
                float toastHeight = 24f;
                float toastX = anchorRect.x + (anchorRect.width - toastWidth) / 2f;
                float toastY = anchorRect.yMax + yOffset;
                Rect toastRect = new Rect(toastX, toastY, toastWidth, toastHeight);

                Color bg = new Color(0.06f, 0.08f, 0.12f, 0.92f * alpha);
                Color border = new Color(Color.r, Color.g, Color.b, 0.95f * alpha);
                Color textCol = new Color(Color.r, Color.g, Color.b, alpha);

                DrawRect(toastRect, bg);
                DrawBorder(toastRect, border, 1);

                Color prevCol = style.normal.textColor;
                style.normal.textColor = textCol;
                GUI.Label(toastRect, Message, style);
                style.normal.textColor = prevCol;
            }
        }

        // =====================================================================
        // 3. Centralized Cursor Token Manager
        // =====================================================================

        private static readonly HashSet<object> _cursorRequesters = new HashSet<object>();
        private static readonly CursorCapture _cursorCapture = new CursorCapture();

        public static void AcquireCursor(object requester)
        {
            if (requester == null) return;
            if (_cursorRequesters.Add(requester) && _cursorRequesters.Count == 1)
            {
                _cursorCapture.Capture();
            }
        }

        public static void ReleaseCursor(object requester)
        {
            if (requester == null) return;
            if (_cursorRequesters.Remove(requester) && _cursorRequesters.Count == 0)
            {
                _cursorCapture.Release();
            }
        }

        public static void ClearAllCursorRequesters()
        {
            _cursorRequesters.Clear();
            _cursorCapture.Release();
        }

        public static void DrawTopCursor(Rect? boundingBox = null, float scale = 1.0f, Vector2? pivot = null)
        {
            GUIHelper.DrawTopCursor(boundingBox, scale, pivot);
        }

        // =====================================================================
        // 4. Modal Menu & Escape Key Management
        // =====================================================================

        private static float _lastEscapeCloseTime = -10f;

        public static void NotifyEscapeClosed()
        {
            _lastEscapeCloseTime = Time.unscaledTime;
        }

        public static bool IsAnyMenuOpen()
        {
            return CasinoRouletteUI.IsOpen || 
                   (AdminMenuUI.Instance != null && AdminMenuUI.Instance.IsOpen) || 
                   NotificationJournalUI.IsOpen;
        }

        public static bool ShouldSuppressPauseScreen()
        {
            if (IsAnyMenuOpen()) return true;
            if (Time.unscaledTime - _lastEscapeCloseTime < 0.30f) return true;
            return false;
        }

        public static void CloseOpenMenus()
        {
            if (CasinoRouletteUI.IsOpen)
            {
                CasinoRouletteUI.Instance?.Close();
            }
            if (AdminMenuUI.Instance != null && AdminMenuUI.Instance.IsOpen)
            {
                AdminMenuUI.Instance.SetOpen(false);
            }
            if (NotificationJournalUI.IsOpen)
            {
                NotificationJournalUI.Instance?.SetOpen(false);
            }
            NotifyEscapeClosed();
        }

        // =====================================================================
        // 5. Singleplayer Pause Coordination
        // =====================================================================
        private static bool _pausedByUs = false;
        private static float _savedTimeScale = 1f;

        public static bool IsAnyRouletteOrMenuOpen()
        {
            if (CasinoRouletteUI.IsOpen || CasinoRouletteUI.IsBannerActive) return true;
            if (AdminMenuUI.Instance != null && AdminMenuUI.Instance.IsOpen) return true;
            if (NotificationJournalUI.IsOpen) return true;
            if (RouletteUI.Instance != null && RouletteUI.Instance.IsSpinningOrBannerOpen) return true;
            return false;
        }

        public static void UpdateSinglePlayerPause()
        {
            if (ModConfig.PauseGameWhileOpen == null || !ModConfig.PauseGameWhileOpen.Value || !Run.instance || !RoR2Application.isInSinglePlayer)
            {
                if (_pausedByUs)
                {
                    RestoreSinglePlayerPause();
                }
                return;
            }

            bool want = IsAnyRouletteOrMenuOpen();

            if (want && !_pausedByUs)
            {
                if (PauseManager.isPaused || Time.timeScale == 0f)
                {
                    return;
                }

                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _pausedByUs = true;
            }
            else if (!want && _pausedByUs)
            {
                if (PauseManager.isPaused)
                {
                    return;
                }

                RestoreSinglePlayerPause();
            }
        }

        public static void RestoreSinglePlayerPause()
        {
            if (_pausedByUs)
            {
                if (Time.timeScale == 0f)
                {
                    Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
                }
                _pausedByUs = false;
            }
        }

        // =====================================================================
        // 6. Memory Cleanup
        // =====================================================================

        public static void Cleanup()
        {
            RestoreSinglePlayerPause();
            ClearAllCursorRequesters();
            GUIHelper.Cleanup();
        }
    }
}
