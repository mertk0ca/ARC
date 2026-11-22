using UnityEngine;

static class UiSpriteFactory
{
    public static Sprite Disc(int size, Color color)
    {
        Texture2D texture = NewTexture(size, size);
        float radius = size * 0.5f - 1f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float a = Mathf.Clamp01(radius - d);
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
        }

        return ToSprite(texture);
    }

    public static Sprite Ring(int size, float innerRatio, Color color)
    {
        Texture2D texture = NewTexture(size, size);
        float outer = size * 0.5f - 1.5f;
        float inner = outer * innerRatio;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                a = Mathf.SmoothStep(0f, 1f, a);
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
        }

        return ToSprite(texture);
    }

    public static Sprite SplitNightDayArc(int size, float innerRatio)
    {
        Texture2D texture = NewTexture(size, size);
        float outer = size * 0.5f - 1.5f;
        float inner = outer * innerRatio;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        Color night = new Color(0.38f, 0.42f, 0.5f, 1f);
        Color day = new Color(0.86f, 0.78f, 0.58f, 1f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, center);
                float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                a = Mathf.SmoothStep(0f, 1f, a);
                if (p.y < center.y)
                    a = 0f;
                Color c = p.x < center.x ? night : day;
                float seam = 1f - Mathf.Clamp01(Mathf.Abs(p.x - center.x) / 2.2f);
                if (seam > 0f && p.y >= center.y)
                    c = Color.Lerp(c, new Color(0.55f, 0.55f, 0.55f, 1f), seam * 0.35f);
                texture.SetPixel(x, y, new Color(c.r, c.g, c.b, c.a * a));
            }
        }

        return ToSprite(texture);
    }

    public static Sprite RoundRect(int width, int height, int radius, Color color)
    {
        Texture2D texture = NewTexture(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Min(x, width - 1 - x);
                float dy = Mathf.Min(y, height - 1 - y);
                float a = 1f;
                if (dx < radius && dy < radius)
                {
                    float d = Vector2.Distance(new Vector2(dx, dy), new Vector2(radius, radius));
                    a = Mathf.Clamp01(radius - d);
                }

                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
        }

        return ToSprite(texture);
    }

    public static Sprite Sun(int size)
    {
        Texture2D texture = NewTexture(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float core = size * 0.22f;
        float rayInner = size * 0.28f;
        float rayOuter = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = p.magnitude;
                float coreA = Mathf.Clamp01(core - d + 1f);
                float angle = Mathf.Atan2(p.y, p.x);
                float spokes = Mathf.Abs(Mathf.Cos(angle * 4f));
                float rayBand = Mathf.Clamp01(d - rayInner) * Mathf.Clamp01(rayOuter - d);
                float rayA = rayBand * Mathf.SmoothStep(0.35f, 1f, spokes) * 0.95f;
                float glow = Mathf.Clamp01(1f - d / (size * 0.48f)) * 0.25f;
                float a = Mathf.Max(coreA, Mathf.Max(rayA, glow));
                Color c = Color.Lerp(new Color(1f, 0.55f, 0.12f, a), new Color(1f, 0.95f, 0.45f, a), coreA);
                texture.SetPixel(x, y, c);
            }
        }

        return ToSprite(texture);
    }

    public static Sprite Moon(int size)
    {
        Texture2D texture = NewTexture(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 cut = center + new Vector2(size * 0.16f, size * 0.04f);
        float radius = size * 0.28f;
        float cutRadius = size * 0.24f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float moon = Mathf.Clamp01(radius - Vector2.Distance(p, center) + 1f);
                float hole = Mathf.Clamp01(cutRadius - Vector2.Distance(p, cut) + 1f);
                float a = Mathf.Clamp01(moon - hole);
                Color c = Color.Lerp(new Color(0.45f, 0.55f, 0.85f, a), new Color(0.92f, 0.95f, 1f, a), moon);
                texture.SetPixel(x, y, c);
            }
        }

        return ToSprite(texture);
    }

    static Texture2D NewTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color clear = Color.clear;
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;
        texture.SetPixels(pixels);
        return texture;
    }

    static Sprite ToSprite(Texture2D texture)
    {
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
