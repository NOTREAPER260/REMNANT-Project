using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small helpers for assembling uGUI hierarchies from code.
/// </summary>
/// uGUI object တွေကို code နဲ့ ဆောက်ရတာ ရှည်လျားလို့ ဒီမှာ helper အဖြစ် စုထားပါတယ်။
public static class InventoryUI
{
    /// <summary>The built-in font. Needs no imported asset and no TMP Essentials.</summary>
    public static Font DefaultFont()
    {
        // Unity 6 renamed the built-in Arial to LegacyRuntime.ttf.
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.localScale = Vector3.one;
        return rect;
    }

    /// <summary>Anchor a rect to fill its parent, with an optional uniform inset.</summary>
    public static RectTransform Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        return rect;
    }

    public static Image AddImage(RectTransform rect, Sprite sprite, Color color, Image.Type type, bool raycast)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        image.raycastTarget = raycast;
        if (type == Image.Type.Sliced || type == Image.Type.Tiled)
        {
            image.pixelsPerUnitMultiplier = 1f;
        }
        return image;
    }

    public static Text AddText(RectTransform rect, Font font, string content, int size,
                               TextAnchor anchor, Color color, FontStyle style)
    {
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        return text;
    }

    /// <summary>
    /// Spaces out a short label ("TAG" -> "T A G"). uGUI's legacy Text has no
    /// letter-spacing, and wide tracking is half of what makes a UI feel clinical.
    /// </summary>
    /// Legacy Text မှာ letter-spacing မရှိလို့ စာလုံးကြားမှာ space ကို လက်နဲ့ထည့်ပေးတာပါ။
    public static string Track(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder(value.Length * 2);
        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }
            sb.Append(value[i]);
        }
        return sb.ToString();
    }
}
