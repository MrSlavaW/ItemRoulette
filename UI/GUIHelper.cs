using System.Collections.Generic;
using ItemRoulette.Configuration;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ItemRoulette.UI
{
    public static class GUIHelper
    {
        private static readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

        private static readonly System.Reflection.FieldInfo? _chatBoxInstanceField = typeof(ChatBox).GetField("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        /// <summary>
        /// Returns true if the user is currently typing into chat, console, or any UI input field,
        /// or if a modal pause/menu screen is open.
        /// Hotkeys and input triggers MUST check this to avoid triggering while typing.
        /// </summary>
        public static bool IsTypingOrInputBlocked()
        {
            // 1. RoR2 ChatBox active (returns true whenever chat input is open)
            if (ChatBox.IsActive)
            {
                return true;
            }

            if (_chatBoxInstanceField != null)
            {
                try
                {
                    if (_chatBoxInstanceField.GetValue(null) is ChatBox cb && cb != null)
                    {
                        if (cb.inputField != null && cb.inputField.isFocused)
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }

            // 2. RoR2 Console Window active
            if (ConsoleWindow.instance != null && ConsoleWindow.instance.gameObject != null && ConsoleWindow.instance.gameObject.activeInHierarchy)
            {
                return true;
            }

            // 3. Unity IMGUI keyboard focus (e.g. textfield inside our or other mod's IMGUI windows)
            if (GUIUtility.keyboardControl != 0)
            {
                return true;
            }

            // 4. Unity EventSystems input fields (TMP_InputField or UnityEngine.UI.InputField)
            var currentEventSystem = EventSystem.current;
            if (currentEventSystem != null && currentEventSystem.currentSelectedGameObject != null)
            {
                var sel = currentEventSystem.currentSelectedGameObject;
                if (sel.GetComponent<TMP_InputField>() != null || sel.GetComponent<UnityEngine.UI.InputField>() != null)
                {
                    return true;
                }
            }

            // 5. RoR2 MPEventSystems
            if (MPEventSystem.readOnlyInstancesList != null)
            {
                for (int i = 0; i < MPEventSystem.readOnlyInstancesList.Count; i++)
                {
                    var mp = MPEventSystem.readOnlyInstancesList[i];
                    if (mp != null && mp.currentSelectedGameObject != null)
                    {
                        var sel = mp.currentSelectedGameObject;
                        if (sel.GetComponent<TMP_InputField>() != null || sel.GetComponent<UnityEngine.UI.InputField>() != null)
                        {
                            return true;
                        }
                    }
                }
            }

            // 6. RoR2 Pause Screen active
            if (PauseScreenController.instancesList != null)
            {
                for (int i = 0; i < PauseScreenController.instancesList.Count; i++)
                {
                    var p = PauseScreenController.instancesList[i];
                    if (p != null && p.gameObject != null && p.gameObject.activeInHierarchy)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static float SafeUiScale => Mathf.Clamp(ModConfig.UiScale?.Value ?? 1.0f, 0.1f, 5.0f);

        public readonly struct GuiScaleScope : System.IDisposable
        {
            private readonly Matrix4x4 _previousMatrix;
            private readonly bool _active;

            public GuiScaleScope(float scale, Vector2 pivot)
            {
                if (Mathf.Abs(scale - 1.0f) > 0.001f && scale > 0.001f)
                {
                    _previousMatrix = GUI.matrix;
                    _active = true;
                    GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);
                }
                else
                {
                    _previousMatrix = Matrix4x4.identity;
                    _active = false;
                }
            }

            public void Dispose()
            {
                if (_active)
                {
                    GUI.matrix = _previousMatrix;
                }
            }
        }

        public static GuiScaleScope UseScale(float scale, Vector2 pivot) => new GuiScaleScope(scale, pivot);

        public static Texture2D GetSolidTexture(Color color)
        {
            string key = $"solid_{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}";
            if (_textureCache.TryGetValue(key, out var tex) && tex != null)
            {
                return tex;
            }

            tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            _textureCache[key] = tex;
            return tex;
        }

        public static Texture2D CreateBorderedTexture(int width, int height, Color fill, Color border, int borderWidth = 2)
        {
            string key = $"border_{width}_{height}_{fill.GetHashCode()}_{border.GetHashCode()}_{borderWidth}";
            if (_textureCache.TryGetValue(key, out var tex) && tex != null)
            {
                return tex;
            }

            tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = (x < borderWidth || x >= width - borderWidth || y < borderWidth || y >= height - borderWidth);
                    pixels[y * width + x] = isBorder ? border : fill;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _textureCache[key] = tex;
            return tex;
        }

        public static void DrawBorder(Rect rect, Color color, int thickness = 2)
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

        public static void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        public static void Cleanup()
        {
            foreach (var kvp in _textureCache)
            {
                if (kvp.Value != null)
                {
                    Object.Destroy(kvp.Value);
                }
            }
            _textureCache.Clear();

            if (_cursorTexture != null && !_isNativeCursorLoaded)
            {
                Object.Destroy(_cursorTexture);
            }
            _cursorTexture = null;
            _isNativeCursorLoaded = false;
        }

        public static Texture2D CreateDiagonalSplitTexture(int width, int height, Color bottomLeftColor, Color topRightColor, Color dividerColor, int dividerThickness = 2)
        {
            string key = $"split_{width}_{height}_{bottomLeftColor.GetHashCode()}_{topRightColor.GetHashCode()}_{dividerColor.GetHashCode()}_{dividerThickness}";
            if (_textureCache.TryGetValue(key, out var tex) && tex != null)
            {
                return tex;
            }

            tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int lineDist = (x + y) - (height - 1);
                    if (Mathf.Abs(lineDist) <= dividerThickness)
                    {
                        pixels[y * width + x] = dividerColor;
                    }
                    else if (lineDist > 0)
                    {
                        pixels[y * width + x] = topRightColor;
                    }
                    else
                    {
                        pixels[y * width + x] = bottomLeftColor;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _textureCache[key] = tex;
            return tex;
        }
        public static Texture2D CreateDiagonalOverlayTexture(int width, int height, Color topRightColor, Color dividerColor, int dividerThickness = 2, float cutoffRatio = 0.5f)
        {
            string key = $"overlay_{width}_{height}_{topRightColor.GetHashCode()}_{dividerColor.GetHashCode()}_{dividerThickness}_{cutoffRatio:F2}";
            if (_textureCache.TryGetValue(key, out var tex) && tex != null)
            {
                return tex;
            }

            tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[width * height];
            Color transparent = new Color(0f, 0f, 0f, 0f);

            int cutoff = Mathf.RoundToInt((width + height) * cutoffRatio) - 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int lineDist = (x + y) - cutoff;
                    if (Mathf.Abs(lineDist) <= dividerThickness)
                    {
                        pixels[y * width + x] = dividerColor;
                    }
                    else if (lineDist > 0)
                    {
                        pixels[y * width + x] = topRightColor;
                    }
                    else
                    {
                        pixels[y * width + x] = transparent;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _textureCache[key] = tex;
            return tex;
        }

        private static Texture2D? _cursorTexture;
        private static Rect _cursorUv = new Rect(0, 0, 1, 1);
        private static Vector2 _cursorHotspot = Vector2.zero;
        private static float _cursorWidth = 24f;
        private static float _cursorHeight = 24f;
        private static bool _isNativeCursorLoaded = false;

        public static void EnsureCursorLoaded()
        {
            if (_cursorTexture != null && _isNativeCursorLoaded) return;

            if (TryLoadNativeRoR2Cursor())
            {
                _isNativeCursorLoaded = true;
                return;
            }

            if (_cursorTexture != null) return;

            _cursorTexture = CreateProceduralCursorTexture();
            _cursorUv = new Rect(0, 0, 1, 1);
            _cursorWidth = 24f;
            _cursorHeight = 24f;
            _cursorHotspot = Vector2.zero;
        }

        private static bool TryLoadNativeRoR2Cursor()
        {
            try
            {
                var controllers = Object.FindObjectsOfType<RoR2.UI.CursorIndicatorController>();
                foreach (var cic in controllers)
                {
                    if (cic != null && cic.mouseCursorSet.pointerObject != null)
                    {
                        var img = cic.mouseCursorSet.pointerObject.GetComponentInChildren<UnityEngine.UI.Image>();
                        if (img != null && img.sprite != null && img.sprite.texture != null)
                        {
                            var sprite = img.sprite;
                            _cursorTexture = sprite.texture;
                            float texW = sprite.texture.width;
                            float texH = sprite.texture.height;
                            Rect r = sprite.rect;
                            _cursorUv = new Rect(r.x / texW, r.y / texH, r.width / texW, r.height / texH);

                            // Match native pointer size on screen (around 22-24px)
                            float displayW = 24f;
                            float displayH = 24f;
                            if (img.rectTransform != null && img.rectTransform.rect.width > 0)
                            {
                                displayW = img.rectTransform.rect.width;
                                displayH = img.rectTransform.rect.height;
                            }
                            else if (r.width > 0 && r.width <= 32f)
                            {
                                displayW = r.width;
                                displayH = r.height;
                            }

                            _cursorWidth = Mathf.Clamp(displayW, 18f, 26f);
                            _cursorHeight = Mathf.Clamp(displayH, 18f, 26f);
                            // Hotspot of a pointer arrow is strictly at its top-left tip (0, 0)
                            _cursorHotspot = Vector2.zero;
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static void DrawTopCursor(Rect? boundingBox = null, float scale = 1.0f, Vector2? pivot = null)
        {
            Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (boundingBox.HasValue)
            {
                Rect screenBox = boundingBox.Value;
                if (Mathf.Abs(scale - 1.0f) > 0.001f && scale > 0.001f)
                {
                    Vector2 p = pivot ?? screenBox.position;
                    screenBox = new Rect(
                        p.x + (screenBox.x - p.x) * scale,
                        p.y + (screenBox.y - p.y) * scale,
                        screenBox.width * scale,
                        screenBox.height * scale
                    );
                }

                if (!screenBox.Contains(mousePos))
                {
                    return;
                }
            }

            EnsureCursorLoaded();
            if (_cursorTexture == null) return;

            Matrix4x4 prevMatrix = GUI.matrix;
            int prevDepth = GUI.depth;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                GUI.depth = -5000;

                Rect cursorRect = new Rect(
                    mousePos.x - (_cursorHotspot.x * _cursorWidth),
                    mousePos.y - (_cursorHotspot.y * _cursorHeight),
                    _cursorWidth,
                    _cursorHeight
                );

                Color prevColor = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(cursorRect, _cursorTexture, _cursorUv);
                GUI.color = prevColor;
            }
            finally
            {
                GUI.depth = prevDepth;
                GUI.matrix = prevMatrix;
            }
        }

        private static Texture2D CreateProceduralCursorTexture()
        {
            int size = 28;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

            Vector2[] poly = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 21f),
                new Vector2(5.5f, 16.5f),
                new Vector2(9.5f, 25.5f),
                new Vector2(13f, 24f),
                new Vector2(9f, 15f),
                new Vector2(16f, 15f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    if (IsPointInPolygon(pt, poly))
                    {
                        bool isBorder = false;
                        for (int dx = -1; dx <= 1 && !isBorder; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (!IsPointInPolygon(new Vector2(pt.x + dx, pt.y + dy), poly))
                                {
                                    isBorder = true;
                                    break;
                                }
                            }
                        }
                        colors[(size - 1 - y) * size + x] = isBorder ? new Color(0.1f, 0.12f, 0.15f, 0.95f) : new Color(0.85f, 0.88f, 0.92f, 1f);
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        private static bool IsPointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
