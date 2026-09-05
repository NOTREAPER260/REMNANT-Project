using UnityEngine;

/// <summary>
/// Builds a single aged, torn-edged paper sprite for the note/book UI - runtime
/// generated like everything in InventoryTextureFactory, just with an irregular
/// silhouette instead of a clean rectangle.
/// </summary>
/// Note/Book UI အတွက် ဟောင်းနွမ်းနေတဲ့ စာရွက် (အနားစွန်းတွေ စုတ်ပြတ်နေတဲ့ပုံစံ) texture
/// ကို runtime မှာ ကိုယ်တိုင် ဆွဲထုတ်ပေးပါတယ် - art asset မလိုပါ.
public static class NoteTextureFactory
{
    /// <param name="paperColor">Base parchment colour.</param>
    /// <param name="burnColor">Darker colour blended in right at the torn edge.</param>
    /// <param name="topFray">0 = a perfectly straight top edge, 1 = full tear. A page
    /// bound into a book stays straight on its spine side but frays everywhere else.</param>
    /// <param name="bloodAmount">0 = none. Above that, 1-3 splatters with drips are
    /// stamped onto the page - a horror flourish, not part of the paper itself.</param>
    public static Sprite CreateTornPaper(int width, int height, Color paperColor, Color burnColor, int seed,
        float topFray = 1f, float bottomFray = 1f, float leftFray = 1f, float rightFray = 1f,
        float bloodAmount = 0f)
    {
        width = Mathf.Max(32, width);
        height = Mathf.Max(32, height);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] px = new Color[width * height];

        // Distinct offsets per edge/octave so the four sides never sample the
        // same Perlin curve. They MUST stay small: Mathf.PerlinNoise quantises
        // badly once a coordinate grows past a few hundred, and large offsets
        // turn the noise into flat blocks.
        float ns = Noise01(seed) * 64f;
        float sTopBig = ns + 3.1f;
        float sTopFine = ns + 17.9f;
        float sBottomBig = ns + 31.7f;
        float sBottomFine = ns + 44.3f;
        float sLeftBig = ns + 58.9f;
        float sLeftFine = ns + 71.1f;
        float sRightBig = ns + 85.7f;
        float sRightFine = ns + 97.3f;

        float maxDim = Mathf.Max(width, height);
        float bigAmp = maxDim * 0.024f;
        float fineAmp = maxDim * 0.008f;
        float burnWidth = maxDim * 0.030f;
        float feather = Mathf.Max(1.2f, maxDim * 0.012f);

        // Blood splatters: a handful of irregular blobs, each with a drip
        // trailing down from it. Positions/sizes are picked once per texture;
        // the irregular edge itself is still sampled per-pixel from angle noise.
        System.Random bloodRng = new System.Random(seed + 4271);
        int splatterCount = bloodAmount > 0f ? bloodRng.Next(2, 4) : 0;
        float[] splatX = new float[splatterCount];
        float[] splatY = new float[splatterCount];
        float[] splatR = new float[splatterCount];
        float[] splatDrip = new float[splatterCount];
        float[] splatSeed = new float[splatterCount];

        for (int i = 0; i < splatterCount; i++)
        {
            splatX[i] = Mathf.Lerp(width * 0.20f, width * 0.80f, (float)bloodRng.NextDouble());
            splatY[i] = Mathf.Lerp(height * 0.25f, height * 0.85f, (float)bloodRng.NextDouble());
            splatR[i] = maxDim * Mathf.Lerp(0.026f, 0.062f, (float)bloodRng.NextDouble());
            splatDrip[i] = splatR[i] * Mathf.Lerp(1.8f, 4.5f, (float)bloodRng.NextDouble());
            splatSeed[i] = (float)bloodRng.NextDouble() * 40f;
        }

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);

            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);

                float topInset = EdgeInset(u, sTopBig, sTopFine, bigAmp * topFray, fineAmp * topFray);
                float bottomInset = EdgeInset(u, sBottomBig, sBottomFine, bigAmp * bottomFray, fineAmp * bottomFray);
                float leftInset = EdgeInset(v, sLeftBig, sLeftFine, bigAmp * leftFray, fineAmp * leftFray);
                float rightInset = EdgeInset(v, sRightBig, sRightFine, bigAmp * rightFray, fineAmp * rightFray);

                float distTop = (height - 1 - y) - topInset;
                float distBottom = y - bottomInset;
                float distLeft = x - leftInset;
                float distRight = (width - 1 - x) - rightInset;

                float d = Mathf.Min(Mathf.Min(distTop, distBottom), Mathf.Min(distLeft, distRight));

                // A soft feathered edge reads as frayed paper fibre, not a laser-cut vector edge.
                float alpha = Step01(-feather, feather, d);

                // Broad, low-contrast blotches - a faint hint of age, not dirt.
                float stain = Mathf.PerlinNoise(u * 2.6f + ns + 11.3f, v * 2.6f + ns + 26.7f);
                float stainDarken = Mathf.Lerp(0.93f, 1f, stain);

                // Fine high-frequency noise gives the paper its fibre/weave texture.
                float fiber = Mathf.PerlinNoise(u * 55f + ns + 39.1f, v * 55f + ns + 52.9f);
                float fiberDarken = Mathf.Lerp(0.97f, 1f, fiber);

                // A faint top-to-bottom gradient, like soft light falling across the page.
                float lightGrad = Mathf.Lerp(1.02f, 0.985f, v);

                float darken = stainDarken * fiberDarken * lightGrad;
                Color baseColor = new Color(
                    paperColor.r * darken, paperColor.g * darken, paperColor.b * darken, 1f);

                // A gentle, muted tint right at the torn edge - aged, not charred.
                float burnT = 1f - Mathf.Clamp01(d / burnWidth);
                burnT = burnT * burnT * burnT * 0.35f;
                Color c = Color.Lerp(baseColor, burnColor, burnT);

                if (splatterCount > 0)
                {
                    float bloodMask = 0f;

                    for (int i = 0; i < splatterCount; i++)
                    {
                        float dx = x - splatX[i];
                        float dyUp = y - splatY[i];

                        // Angle-based noise perturbs the blob radius so it reads
                        // as an irregular splatter, not a perfect circle.
                        float angle = Mathf.Atan2(dyUp, dx);
                        float edgeNoise = Mathf.PerlinNoise(
                            Mathf.Cos(angle) * 1.5f + splatSeed[i],
                            Mathf.Sin(angle) * 1.5f + splatSeed[i] + 5.3f);
                        float radius = splatR[i] * (0.6f + edgeNoise * 0.75f);

                        float dist = Mathf.Sqrt(dx * dx + dyUp * dyUp);
                        float blobCov = Coverage(dist - radius, 1.4f);

                        // A drip trailing downward (texture row 0 is the bottom,
                        // so "down" is decreasing y) from the blob, tapering out.
                        float below = Mathf.Max(0f, -dyUp);
                        float dripT = Mathf.Clamp01(below / Mathf.Max(splatDrip[i], 0.01f));
                        float dripWidth = Mathf.Lerp(radius * 0.32f, radius * 0.06f, dripT);
                        float dripCov = below > 0f
                            ? Coverage(Mathf.Abs(dx) - dripWidth, 1.2f) * (1f - dripT)
                            : 0f;

                        float cov = Mathf.Max(blobCov, dripCov);
                        if (cov > bloodMask)
                        {
                            bloodMask = cov;
                        }
                    }

                    // Fade out near the torn edge so blood never appears to float past the paper.
                    bloodMask *= Mathf.Clamp01(d / (feather * 3f)) * bloodAmount;

                    if (bloodMask > 0f)
                    {
                        Color blood = new Color(0.32f, 0.025f, 0.020f, 1f);
                        c = Color.Lerp(c, blood, bloodMask);
                    }
                }

                px[y * width + x] = new Color(c.r, c.g, c.b, alpha);
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    /// <summary>
    /// The book itself: a rounded leather cover with a brass trim line, metal
    /// corner brackets, a clasp on each side and a ridged spine down the middle.
    /// Drawn with signed-distance shapes, the same way InventoryTextureFactory
    /// draws its flashlight icon.
    /// </summary>
    /// စာအုပ်ရဲ့ အဖုံး - သားရေ cover, ကြေးဝါ trim လိုင်း, ထောင့်လေးထောင့်က သတ္တုကွက်,
    /// ဘေးနှစ်ဖက်က သော့ချိတ်, အလယ်မှာ spine. အားလုံး SDF ပုံသဏ္ဍာန်တွေနဲ့ ဆွဲထားတာပါ.
    public static Sprite CreateBookCover(int width, int height, Color leather, Color trim, Color metal, int seed)
    {
        width = Mathf.Max(64, width);
        height = Mathf.Max(64, height);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] px = new Color[width * height];

        float cx = (width - 1) * 0.5f;
        float cy = (height - 1) * 0.5f;
        float aa = 1.2f;
        float minDim = Mathf.Min(width, height);

        float margin = Mathf.Max(2f, width * 0.006f);
        float coverHalfW = cx - margin;
        float coverHalfH = cy - margin;
        float cornerR = minDim * 0.055f;

        float trimInset = minDim * 0.032f;
        float trimHalf = Mathf.Max(1f, minDim * 0.0045f);

        float spineHalfW = width * 0.020f;
        float bracketSize = minDim * 0.115f;
        float bracketThick = minDim * 0.030f;
        float claspHalfW = width * 0.016f;
        float claspHalfH = height * 0.080f;

        // Perlin coordinates must stay small, or the noise quantises into blocks.
        float ns = Noise01(seed) * 64f;

        // The recess the pages sit in, so they read as seated inside the boards.
        float wellInset = minDim * 0.075f;
        float wellFalloff = minDim * 0.045f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color acc = new Color(0f, 0f, 0f, 0f);

                float dCover = RoundedBox(x, y, cx, cy, coverHalfW, coverHalfH, cornerR);
                float coverA = Coverage(dCover, aa);

                if (coverA > 0f)
                {
                    // Two octaves of noise stand in for leather grain.
                    float g1 = Mathf.PerlinNoise(x * 0.035f + ns + 5.7f, y * 0.035f + ns + 19.3f);
                    float g2 = Mathf.PerlinNoise(x * 0.160f + ns + 33.1f, y * 0.160f + ns + 47.9f);
                    float shade = Mathf.Lerp(0.82f, 1.10f, g1) * Mathf.Lerp(0.94f, 1.06f, g2);

                    // The rounded outer edge falls away into shadow.
                    shade *= Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(-dCover / (cornerR * 0.9f)));

                    // The inner recess the pages drop into - a soft seam of shadow
                    // at its lip, not a black halo around the paper.
                    float dWell = RoundedBox(x, y, cx, cy,
                        coverHalfW - wellInset, coverHalfH - wellInset, cornerR * 0.5f);
                    shade *= Mathf.Lerp(0.70f, 1f, Mathf.Clamp01(Mathf.Abs(dWell) / wellFalloff));

                    acc = Over(acc, new Color(leather.r * shade, leather.g * shade, leather.b * shade, 1f), coverA);
                }

                // Brass trim line running parallel to the cover's outline.
                float trimA = Band(dCover, -trimInset, trimHalf, aa) * coverA;
                acc = Over(acc, trim, trimA * 0.9f);

                // Spine strip down the middle, darker than the boards either side.
                float dSpine = RoundedBox(x, y, cx, cy, spineHalfW, coverHalfH - trimInset * 0.5f, spineHalfW * 0.6f);
                float spineA = Coverage(dSpine, aa);
                if (spineA > 0f)
                {
                    float sg = Mathf.PerlinNoise(x * 0.09f + ns + 61.3f, y * 0.09f + ns + 74.7f);
                    float ss = Mathf.Lerp(0.52f, 0.82f, sg);
                    acc = Over(acc, new Color(leather.r * ss, leather.g * ss, leather.b * ss, 1f), spineA);

                    float bandTop = Band(y, cy + coverHalfH * 0.62f, height * 0.009f, aa);
                    float bandBottom = Band(y, cy - coverHalfH * 0.62f, height * 0.009f, aa);
                    acc = Over(acc, trim, Mathf.Max(bandTop, bandBottom) * spineA * 0.85f);
                }

                // Metal fittings at all four corners.
                float bracket = 0f;
                bracket = Mathf.Max(bracket, CornerBracket(x, y, cx - coverHalfW, cy - coverHalfH, 1f, 1f, bracketSize, bracketThick, aa));
                bracket = Mathf.Max(bracket, CornerBracket(x, y, cx + coverHalfW, cy - coverHalfH, -1f, 1f, bracketSize, bracketThick, aa));
                bracket = Mathf.Max(bracket, CornerBracket(x, y, cx - coverHalfW, cy + coverHalfH, 1f, -1f, bracketSize, bracketThick, aa));
                bracket = Mathf.Max(bracket, CornerBracket(x, y, cx + coverHalfW, cy + coverHalfH, -1f, -1f, bracketSize, bracketThick, aa));

                // A clasp strap halfway up each side.
                float clasp = 0f;
                clasp = Mathf.Max(clasp, Coverage(RoundedBox(x, y,
                    cx - coverHalfW + claspHalfW * 0.4f, cy, claspHalfW, claspHalfH, claspHalfW * 0.55f), aa));
                clasp = Mathf.Max(clasp, Coverage(RoundedBox(x, y,
                    cx + coverHalfW - claspHalfW * 0.4f, cy, claspHalfW, claspHalfH, claspHalfW * 0.55f), aa));

                float metalA = Mathf.Max(bracket, clasp) * coverA;
                if (metalA > 0f)
                {
                    float m = Mathf.Lerp(0.80f, 1.18f,
                        Mathf.PerlinNoise(x * 0.07f + ns + 88.1f, y * 0.07f + ns + 12.7f));
                    acc = Over(acc, new Color(metal.r * m, metal.g * m, metal.b * m, 1f), metalA);
                }

                px[y * width + x] = acc;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    /// An L-shaped corner fitting: two arms reaching inward from one corner.
    /// <param name="dirX">+1 when the corner is on the left, -1 on the right.</param>
    private static float CornerBracket(float x, float y, float cornerX, float cornerY,
                                       float dirX, float dirY, float size, float thickness, float aa)
    {
        float half = size * 0.5f;
        float t = thickness * 0.5f;

        float armH = Coverage(RoundedBox(x, y,
            cornerX + dirX * half, cornerY + dirY * t, half, t, t * 0.7f), aa);
        float armV = Coverage(RoundedBox(x, y,
            cornerX + dirX * t, cornerY + dirY * half, t, half, t * 0.7f), aa);

        return Mathf.Max(armH, armV);
    }

    /// Signed distance to a rounded box, in pixels.
    private static float RoundedBox(float px, float py, float cx, float cy, float hx, float hy, float r)
    {
        float ax = Mathf.Abs(px - cx) - (hx - r);
        float ay = Mathf.Abs(py - cy) - (hy - r);
        float ox = Mathf.Max(ax, 0f);
        float oy = Mathf.Max(ay, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(ax, ay), 0f) - r;
    }

    private static float Coverage(float signedDistance, float aa)
    {
        return 1f - Step01(-aa, aa, signedDistance);
    }

    /// Coverage of a thin band centred on <paramref name="centre"/>.
    private static float Band(float value, float centre, float halfWidth, float aa)
    {
        return Coverage(Mathf.Abs(value - centre) - halfWidth, aa);
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

    /// <summary>
    /// A horizontal gradient, dark at the centre and fading to transparent at
    /// both edges - stretched into a tall, narrow rect it becomes the shadow
    /// pooling in the crease where a book's two pages meet.
    /// </summary>
    /// အလယ်မှာ အမှောင်ဆုံးဖြစ်ပြီး ဘေးနှစ်ဖက် ပျောက်သွားတဲ့ gradient တစ်ခုပါ. စာအုပ်ရဲ့
    /// စာရွက်နှစ်ရွက် ဆုံရာနေရာက အရိပ်ကို ဒီနည်းနဲ့ ဖန်တီးထားပါတယ်.
    public static Sprite CreateCenterFade(int size, Color color, float maxAlpha)
    {
        Texture2D tex = new Texture2D(size, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] px = new Color[size];
        float c = (size - 1) * 0.5f;
        for (int x = 0; x < size; x++)
        {
            float t = Mathf.Abs(x - c) / Mathf.Max(c, 1e-5f);
            float a = Mathf.Clamp01((1f - t) * maxAlpha);
            px[x] = new Color(color.r, color.g, color.b, a);
        }

        tex.SetPixels(px);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, 1),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    /// <summary>
    /// Two-octave noise-based inset (in pixels) along one edge - a slow, big
    /// curl plus a finer, higher-frequency nick, so the tear reads as paper
    /// fibre rather than a single smooth wave.
    /// </summary>
    private static float EdgeInset(float t, float seedBig, float seedFine, float bigAmp, float fineAmp)
    {
        float big = (Mathf.PerlinNoise(t * 2.2f, seedBig) - 0.5f) * 2f * bigAmp;
        float fine = (Mathf.PerlinNoise(t * 11f, seedFine) - 0.5f) * 2f * fineAmp;
        return Mathf.Max(0f, bigAmp * 0.5f + big + fine);
    }

    /// <summary>
    /// Hashes any seed into 0..1. Perlin coordinates have to stay small - a raw
    /// seed used as an offset loses float precision and flattens the noise into
    /// visible blocks - so every generator here routes its seed through this.
    /// </summary>
    /// Seed ကို 0..1 ကြားကို ပြောင်းပေးတာပါ. Perlin ရဲ့ coordinate ကြီးလွန်းရင်
    /// float precision ဆုံးရှုံးပြီး noise က လေးထောင့်ကွက်တွေ ဖြစ်သွားပါတယ်.
    private static float Noise01(int seed)
    {
        uint h = (uint)seed * 2654435761u;
        h ^= h >> 15;
        h *= 2246822519u;
        h ^= h >> 13;
        return (h % 100000u) / 100000f;
    }

    /// <summary>
    /// GLSL-style smoothstep: 0 below edge0, 1 above edge1, eased in between.
    /// NOT Unity's Mathf.SmoothStep - see InventoryTextureFactory's own note on this.
    /// </summary>
    private static float Step01(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / Mathf.Max(edge1 - edge0, 1e-6f));
        return t * t * (3f - 2f * t);
    }
}
