using UnityEngine;

/// <summary>
/// Builds every sprite the inventory needs at runtime, so the UI works
/// without importing a single art asset.
/// </summary>
/// အနုပညာ asset မလိုအောင် texture အားလုံးကို code နဲ့ပဲ ဆွဲထုတ်ပါတယ်။
/// Project ထဲ ဘာမှ ထပ်ထည့်စရာမလိုတော့လို့ import error ဖြစ်စရာ အကြောင်းမရှိပါဘူး။
public static class InventoryTextureFactory
{
    private const float Aa = 1.6f; // anti-alias width in pixels

    // ---------------------------------------------------------------- panels

    /// <summary>
    /// A 9-sliced frame: solid fill, a hard outline, and a soft inner shadow
    /// so slots read as recessed holes rather than flat squares.
    /// </summary>
    public static Sprite CreateFrame(int size, Color fill, Color edge, int edgeWidth, int shadowWidth)
    {
        Texture2D tex = NewTexture(size, size, FilterMode.Bilinear, TextureWrapMode.Clamp);
        Color[] px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));

                Color c;
                if (d < edgeWidth)
                {
                    c = edge;
                }
                else if (shadowWidth > 0 && d < edgeWidth + shadowWidth)
                {
                    float t = (d - edgeWidth) / (float)shadowWidth;
                    Color shade = new Color(edge.r * 0.35f, edge.g * 0.35f, edge.b * 0.35f, fill.a);
                    c = Color.Lerp(shade, fill, t * t);
                }
                else
                {
                    c = fill;
                }

                px[y * size + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        int border = edgeWidth + shadowWidth + 2;
        return MakeSprite(tex, new Vector4(border, border, border, border));
    }

    /// <summary>Flat 4x4 sprite, for bars, rules and tags.</summary>
    public static Sprite CreateSolid(Color color)
    {
        Texture2D tex = NewTexture(4, 4, FilterMode.Bilinear, TextureWrapMode.Clamp);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++)
        {
            px[i] = color;
        }
        tex.SetPixels(px);
        tex.Apply();
        return MakeSprite(tex, Vector4.zero);
    }

    // ------------------------------------------------------------ atmosphere

    /// <summary>Repeating film grain. Draw it tiled over the panel.</summary>
    public static Sprite CreateGrain(int size, float strength, int seed)
    {
        Texture2D tex = NewTexture(size, size, FilterMode.Point, TextureWrapMode.Repeat);
        Color[] px = new Color[size * size];
        System.Random rng = new System.Random(seed);

        for (int i = 0; i < px.Length; i++)
        {
            float n = (float)rng.NextDouble();
            // bias towards dark specks so the grain dirties rather than brightens
            float a = Mathf.Pow(n, 2.2f) * strength;
            px[i] = new Color(0.05f, 0.05f, 0.05f, a);
        }

        tex.SetPixels(px);
        tex.Apply();
        return MakeSprite(tex, Vector4.zero);
    }

    /// <summary>Radial darkening, stretched over the whole screen.</summary>
    public static Sprite CreateVignette(int size, float inner, float outer, float maxAlpha)
    {
        Texture2D tex = NewTexture(size, size, FilterMode.Bilinear, TextureWrapMode.Clamp);
        Color[] px = new Color[size * size];
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float r = Mathf.Sqrt(nx * nx + ny * ny) / 1.41421f;
                float a = Step01(inner, outer, r) * maxAlpha;
                px[y * size + x] = new Color(0f, 0f, 0f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return MakeSprite(tex, Vector4.zero);
    }

    // ----------------------------------------------------------------- icons

    /// <summary>Side-on flashlight silhouette with a lens and a short beam.</summary>
    public static Sprite CreateFlashlightIcon(int size, Color body, Color lens, Color beam)
    {
        Texture2D tex = NewTexture(size, size, FilterMode.Bilinear, TextureWrapMode.Clamp);
        Color[] px = new Color[size * size];
        float aa = Aa / size;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;
                Color acc = new Color(0f, 0f, 0f, 0f);

                // barrel
                float dBody = RoundedBox(u, v, 0.34f, 0.5f, 0.19f, 0.105f, 0.045f);
                acc = Over(acc, body, Coverage(dBody, aa));

                // two grip rings, slightly darker than the barrel
                if (Coverage(dBody, aa) > 0.5f)
                {
                    Color ring = new Color(body.r * 0.55f, body.g * 0.55f, body.b * 0.55f, 1f);
                    float rA = Band(u, 0.265f, 0.016f, aa);
                    float rB = Band(u, 0.315f, 0.016f, aa);
                    acc = Over(acc, ring, Mathf.Max(rA, rB));
                }

                // head: a trapezoid flaring out towards the lens
                float dHead = Trapezoid(u, v, 0.52f, 0.70f, 0.115f, 0.175f);
                acc = Over(acc, body, Coverage(dHead, aa));

                // lens cap
                float dLens = RoundedBox(u, v, 0.715f, 0.5f, 0.022f, 0.175f, 0.018f);
                acc = Over(acc, lens, Coverage(dLens, aa));

                // three beam arcs in front of the lens
                if (u > 0.745f)
                {
                    float bx = u - 0.735f;
                    float by = v - 0.5f;
                    float rad = Mathf.Sqrt(bx * bx + by * by);
                    float ang = Mathf.Abs(Mathf.Atan2(by, bx));
                    if (ang < 0.72f)
                    {
                        float fade = 1f - Step01(0.30f, 0.72f, ang);
                        float arc = 0f;
                        arc = Mathf.Max(arc, Band(rad, 0.075f, 0.012f, aa));
                        arc = Mathf.Max(arc, Band(rad, 0.135f, 0.011f, aa));
                        arc = Mathf.Max(arc, Band(rad, 0.195f, 0.010f, aa));
                        acc = Over(acc, beam, arc * fade);
                    }
                }

                px[y * size + x] = acc;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return MakeSprite(tex, Vector4.zero);
    }

    // ------------------------------------------------------------- internals

    private static Texture2D NewTexture(int w, int h, FilterMode filter, TextureWrapMode wrap)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = filter;
        tex.wrapMode = wrap;
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    private static Sprite MakeSprite(Texture2D tex, Vector4 border)
    {
        Sprite s = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        s.hideFlags = HideFlags.HideAndDontSave;
        return s;
    }

    /// Signed distance to a rounded box, in normalised units.
    private static float RoundedBox(float px, float py, float cx, float cy, float hx, float hy, float r)
    {
        float ax = Mathf.Abs(px - cx) - (hx - r);
        float ay = Mathf.Abs(py - cy) - (hy - r);
        float ox = Mathf.Max(ax, 0f);
        float oy = Mathf.Max(ay, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(ax, ay), 0f) - r;
    }

    /// Signed distance to a horizontal trapezoid centred on y = 0.5.
    private static float Trapezoid(float px, float py, float x0, float x1, float h0, float h1)
    {
        float t = Mathf.Clamp01((px - x0) / Mathf.Max(x1 - x0, 1e-5f));
        float half = Mathf.Lerp(h0, h1, t);
        float dy = Mathf.Abs(py - 0.5f) - half;
        float dx = Mathf.Max(x0 - px, px - x1);
        return Mathf.Max(dx, dy);
    }

    /// Coverage of a thin band centred on <paramref name="centre"/>.
    private static float Band(float value, float centre, float halfWidth, float aa)
    {
        return Coverage(Mathf.Abs(value - centre) - halfWidth, aa);
    }

    private static float Coverage(float signedDistance, float aa)
    {
        return 1f - Step01(-aa, aa, signedDistance);
    }

    /// <summary>
    /// GLSL-style smoothstep: 0 below <paramref name="edge0"/>, 1 above
    /// <paramref name="edge1"/>, eased in between.
    /// </summary>
    /// သတိ — Unity ရဲ့ Mathf.SmoothStep(from, to, t) က ဒါနဲ့ လုံးဝမတူပါဘူး။
    /// အဲဒါက from နဲ့ to ကြားကို t နဲ့ interpolate လုပ်တာမို့ ဒီနေရာမှာ သုံးလို့မရပါ။
    private static float Step01(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / Mathf.Max(edge1 - edge0, 1e-6f));
        return t * t * (3f - 2f * t);
    }

    /// Standard source-over alpha compositing.
    private static Color Over(Color dst, Color src, float srcAlpha)
    {
        float a = Mathf.Clamp01(srcAlpha) * src.a;
        if (a <= 0f)
        {
            return dst;
        }

        float outA = a + dst.a * (1f - a);
        if (outA <= 0f)
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        float r = (src.r * a + dst.r * dst.a * (1f - a)) / outA;
        float g = (src.g * a + dst.g * dst.a * (1f - a)) / outA;
        float b = (src.b * a + dst.b * dst.a * (1f - a)) / outA;
        return new Color(r, g, b, outA);
    }
}
