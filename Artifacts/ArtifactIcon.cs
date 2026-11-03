using UnityEngine;

namespace ItemRoulette.Artifacts
{
    /// <summary>
    /// Generates high-quality procedural sprite textures for the Artifact of the Roulette.
    /// Eliminates external asset dependencies while rendering clean, crisp circular roulette icons.
    /// </summary>
    internal static class ArtifactIcon
    {
        private const int Size = 128;
        private const int Pockets = 12;

        public static Sprite Create(bool selected)
        {
            var background = selected ? new Color(0.12f, 0.09f, 0.05f) : new Color(0.14f, 0.14f, 0.14f);
            var rim = selected ? new Color(1.0f, 0.82f, 0.28f) : new Color(0.44f, 0.44f, 0.44f);
            var red = selected ? new Color(0.85f, 0.15f, 0.15f) : new Color(0.28f, 0.28f, 0.28f);
            var dark = selected ? new Color(0.08f, 0.08f, 0.08f) : new Color(0.11f, 0.11f, 0.11f);
            var zeroPocket = selected ? new Color(0.15f, 0.85f, 0.40f) : new Color(0.35f, 0.35f, 0.35f);

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = selected ? "ItemRouletteArtifactSelected" : "ItemRouletteArtifactDeselected",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[Size * Size];
            var half = Size / 2f;

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var dx = (x + 0.5f - half) / half;
                    var dy = (y + 0.5f - half) / half;
                    var radius = Mathf.Sqrt(dx * dx + dy * dy);

                    Color color;
                    if (radius < 0.14f)
                    {
                        color = dark;
                    }
                    else if (radius < 0.28f)
                    {
                        color = rim;
                    }
                    else if (radius < 0.38f)
                    {
                        color = dark;
                    }
                    else if (radius < 0.84f)
                    {
                        var angle = Mathf.Atan2(dy, dx) + Mathf.PI;
                        var pocket = Mathf.FloorToInt(angle / (2f * Mathf.PI) * Pockets) % Pockets;
                        if (pocket == 0)
                        {
                            color = zeroPocket;
                        }
                        else
                        {
                            color = (pocket % 2 == 0) ? red : dark;
                        }
                    }
                    else if (radius < 0.96f)
                    {
                        color = rim;
                    }
                    else
                    {
                        color = background;
                    }

                    color.a = 1f;
                    pixels[y * Size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
