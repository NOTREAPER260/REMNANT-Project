using UnityEngine;

/// <summary>
/// The inventory's colour scheme: bone, rust and near-black.
/// Desaturated on purpose - nothing here should look clean or new.
/// </summary>
/// အရောင်စဉ်တွေ တစ်နေရာတည်းမှာ စုထားတာပါ။ Horror feel အတွက် အရောင်တွေကို
/// မှေးမှိန်အောင် (desaturate) ထားပြီး အနက်ရောင်နဲ့ သံချေးရောင်ကို အခြေခံထားပါတယ်။
public static class InventoryPalette
{
    // backdrop
    public static readonly Color ScreenDim = new Color(0.020f, 0.020f, 0.025f, 0.90f);

    // panel
    public static readonly Color PanelFill = new Color(0.075f, 0.070f, 0.062f, 0.985f);
    public static readonly Color PanelEdge = new Color(0.215f, 0.185f, 0.145f, 1f);

    // slots
    public static readonly Color SlotFill = new Color(0.045f, 0.043f, 0.040f, 1f);
    public static readonly Color SlotEdge = new Color(0.165f, 0.150f, 0.130f, 1f);
    public static readonly Color SlotEdgeHover = new Color(0.430f, 0.345f, 0.215f, 1f);
    public static readonly Color SlotEdgeEquipped = new Color(0.620f, 0.420f, 0.190f, 1f);

    // accents
    public static readonly Color Accent = new Color(0.820f, 0.550f, 0.250f, 1f);
    public static readonly Color AccentDim = new Color(0.310f, 0.205f, 0.095f, 1f);

    // text
    public static readonly Color TextPrimary = new Color(0.800f, 0.770f, 0.710f, 1f);
    public static readonly Color TextDim = new Color(0.420f, 0.400f, 0.370f, 1f);
    public static readonly Color TextFaint = new Color(0.245f, 0.235f, 0.220f, 1f);
    public static readonly Color TextOnAccent = new Color(0.070f, 0.055f, 0.040f, 1f);

    // flashlight icon
    public static readonly Color IconBody = new Color(0.720f, 0.700f, 0.660f, 1f);
    public static readonly Color IconLens = new Color(0.950f, 0.780f, 0.450f, 1f);
    public static readonly Color IconBeam = new Color(0.950f, 0.800f, 0.500f, 0.55f);
}
