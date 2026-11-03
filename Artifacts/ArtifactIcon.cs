using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ItemRoulette.Artifacts
{
    /// <summary>
    /// Loads and manages sprite textures for the Artifact of the Roulette.
    /// Uses RoR2 vanilla artifact aesthetics: deep midnight-indigo base with luminous lavender etchings,
    /// transparent background, and separate Selected / Deselected states.
    /// Loads from disk (Assets/Artifact/) if present, falls back to EmbeddedResource, then to procedural generation.
    /// </summary>
    internal static class ArtifactIcon
    {
        private const int Size = 128;
        private const string SelectedResourceName = "ItemRoulette.Assets.Artifact.texArtifactRouletteSelected.png";
        private const string DeselectedResourceName = "ItemRoulette.Assets.Artifact.texArtifactRouletteDeselected.png";

        public static Sprite Create(bool selected)
        {
            var textureName = selected ? "ItemRouletteArtifactSelected" : "ItemRouletteArtifactDeselected";
            var texture = TryLoadFromDisk(selected, textureName)
                ?? TryLoadFromResource(selected, textureName)
                ?? GenerateFallbackTexture(selected, textureName);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Texture2D? TryLoadFromDisk(bool selected, string textureName)
        {
            try
            {
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(assemblyDir)) return null;

                var fileName = selected ? "texArtifactRouletteSelected.png" : "texArtifactRouletteDeselected.png";
                var diskPath = Path.Combine(assemblyDir, "Assets", "Artifact", fileName);

                if (!File.Exists(diskPath)) return null;

                var bytes = File.ReadAllBytes(diskPath);
                return CreateTextureFromBytes(bytes, textureName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to load artifact icon from disk: {ex.Message}");
                return null;
            }
        }

        private static Texture2D? TryLoadFromResource(bool selected, string textureName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = selected ? SelectedResourceName : DeselectedResourceName;

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return null;

                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return CreateTextureFromBytes(buffer, textureName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to load artifact icon from embedded resource: {ex.Message}");
                return null;
            }
        }

        private static Texture2D CreateTextureFromBytes(byte[] bytes, string textureName)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            ImageConversion.LoadImage(texture, bytes);
            return texture;
        }

        /// <summary>
        /// Procedural fallback matching the vanilla RoR2 artifact palette (deep indigo + luminous lavender)
        /// with transparent background in case asset files cannot be accessed.
        /// </summary>
        private static Texture2D GenerateFallbackTexture(bool selected, string textureName)
        {
            var dark = selected ? new Color(0.06f, 0.03f, 0.20f, 1.0f) : new Color(0.05f, 0.03f, 0.12f, 0.82f);
            var light = selected ? new Color(0.92f, 0.85f, 0.95f, 1.0f) : new Color(0.33f, 0.28f, 0.40f, 0.82f);
            var mid = selected ? new Color(0.48f, 0.35f, 0.65f, 1.0f) : new Color(0.20f, 0.16f, 0.28f, 0.82f);
            var clear = new Color(0f, 0f, 0f, 0f);

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[Size * Size];
            var half = Size / 2f;
            const int pockets = 12;

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var dx = (x + 0.5f - half) / half;
                    var dy = (y + 0.5f - half) / half;
                    var radius = Mathf.Sqrt(dx * dx + dy * dy);

                    Color color;
                    if (radius > 0.88f)
                    {
                        color = clear;
                    }
                    else if (radius > 0.82f)
                    {
                        color = light;
                    }
                    else if (radius > 0.70f)
                    {
                        color = dark;
                    }
                    else if (radius > 0.68f)
                    {
                        color = light;
                    }
                    else if (radius > 0.28f)
                    {
                        var angle = Mathf.Atan2(dy, dx) + Mathf.PI;
                        var pocket = Mathf.FloorToInt(angle / (2f * Mathf.PI) * pockets) % pockets;
                        color = (pocket % 2 == 0) ? dark : mid;

                        // Check sector divider spoke
                        var sectorAngle = (angle / (2f * Mathf.PI) * pockets) % 1.0f;
                        if (sectorAngle < 0.08f || sectorAngle > 0.92f)
                        {
                            color = light;
                        }
                    }
                    else if (radius > 0.24f)
                    {
                        color = light;
                    }
                    else if (radius > 0.12f)
                    {
                        color = dark;
                    }
                    else
                    {
                        color = light;
                    }

                    pixels[y * Size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
