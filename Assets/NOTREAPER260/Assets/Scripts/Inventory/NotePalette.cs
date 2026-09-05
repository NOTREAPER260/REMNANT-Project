using UnityEngine;

/// <summary>
/// Warm, aged-paper colours for anything meant to look like it's written on a
/// physical page - the paper note popup and the book's pages. Deliberately the
/// opposite of InventoryPalette's dark bone-and-rust scheme: this one reads as
/// paper, not as a UI panel.
/// </summary>
/// စာရွက်ပေါ်မှာ တကယ်ရေးထားသလို မြင်ရအောင် အသုံးပြုတဲ့ အရောင်စဉ်ပါ. InventoryPalette
/// ရဲ့ အနက်ရောင် UI panel look နဲ့ တမင်ဆန့်ကျင်ဘက် - ဒါက စက္ကူပေါ်ရေးထားသလို ဖြစ်ရမယ်.
public static class NotePalette
{
    public static readonly Color PaperFill = new Color(0.780f, 0.710f, 0.550f, 1f);
    public static readonly Color PaperEdge = new Color(0.280f, 0.220f, 0.140f, 1f);
    /// The soft tan the torn edges fade toward - PaperEdge is too dark there and reads as scorching.
    public static readonly Color PaperAged = new Color(0.560f, 0.470f, 0.320f, 1f);
    /// Matched to the OpenBook model's own paper so the wash over its page blends in
    /// rather than reading as a sticker laid on top.
    public static readonly Color PageWash = new Color(0.880f, 0.860f, 0.805f, 1f);
    public static readonly Color InkTitle = new Color(0.360f, 0.160f, 0.090f, 1f);
    public static readonly Color InkBody = new Color(0.160f, 0.120f, 0.080f, 1f);
    public static readonly Color InkFaint = new Color(0.400f, 0.330f, 0.230f, 1f);

    // the book's leather cover, framing the two open pages
    public static readonly Color CoverLeather = new Color(0.330f, 0.205f, 0.120f, 1f);
    public static readonly Color CoverTrim = new Color(0.580f, 0.440f, 0.200f, 1f);
    public static readonly Color CoverMetal = new Color(0.520f, 0.430f, 0.260f, 1f);
    public static readonly Color Gutter = new Color(0.050f, 0.030f, 0.020f, 1f);
}
