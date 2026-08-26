using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One inventory cell: recessed frame, icon, name, and the "EQUIPPED" ribbon
/// that marks whatever the player is currently holding.
/// </summary>
/// Slot တစ်ခုချင်းစီရဲ့ မြင်ကွင်း။ ကိုင်ထားတဲ့ပစ္စည်းဆိုရင် အပေါ်မှာ
/// "EQUIPPED" ribbon နဲ့ သံချေးရောင် ဘောင်ကို ပြပါတယ်။
public class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private int _index;
    private Image _frame;
    private Image _icon;
    private Text _name;
    private Text _number;
    private RectTransform _ribbon;
    private Image _corner;

    private InventoryItem _item;
    private bool _hovered;
    private bool _selected;

    private System.Action<int, bool> _onHover;
    private System.Action<int> _onClick;

    public int Index { get { return _index; } }
    public InventoryItem Item { get { return _item; } }

    /// <param name="cell">Cell size in pixels; the inner layout is derived from it.</param>
    /// <param name="scale">Font multiplier, shared with the rest of the panel.</param>
    public static InventorySlotView Build(Transform parent, int index, Font font,
                                          Sprite frameSprite, Sprite solidSprite,
                                          float cell, float scale,
                                          System.Action<int, bool> onHover,
                                          System.Action<int> onClick)
    {
        RectTransform root = InventoryUI.NewRect("Slot " + (index + 1).ToString("00"), parent);
        InventorySlotView view = root.gameObject.AddComponent<InventorySlotView>();
        view._index = index;
        view._onHover = onHover;
        view._onClick = onClick;

        // Everything inside a slot is a fraction of the cell, so changing the
        // panel size never leaves the contents behind.
        float ribbonH = cell * 0.145f;
        float iconSize = cell * 0.70f;
        float nameH = cell * 0.135f;
        float inset = cell * 0.022f;

        // recessed frame - the only raycast target, so hover covers the whole cell
        view._frame = InventoryUI.AddImage(root, frameSprite, InventoryPalette.SlotEdge,
            Image.Type.Sliced, true);

        // slot number, hidden once a ribbon takes the top edge
        RectTransform number = InventoryUI.NewRect("Number", root);
        number.anchorMin = new Vector2(0f, 1f);
        number.anchorMax = new Vector2(0f, 1f);
        number.pivot = new Vector2(0f, 1f);
        number.anchoredPosition = new Vector2(inset * 2.6f, -inset * 2f);
        number.sizeDelta = new Vector2(40f * scale, 20f * scale);
        view._number = InventoryUI.AddText(number, font, (index + 1).ToString("00"), Size(14, scale),
            TextAnchor.UpperLeft, InventoryPalette.TextFaint, FontStyle.Normal);

        // icon
        RectTransform icon = InventoryUI.NewRect("Icon", root);
        icon.anchorMin = new Vector2(0.5f, 0.5f);
        icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        // A side-on flashlight only fills the middle band of a square sprite,
        // so the rect is oversized to keep the silhouette readable in the cell.
        icon.anchoredPosition = new Vector2(0f, cell * 0.015f);
        icon.sizeDelta = new Vector2(iconSize, iconSize);
        view._icon = InventoryUI.AddImage(icon, null, Color.clear, Image.Type.Simple, false);
        view._icon.preserveAspect = true;

        // item name along the bottom
        RectTransform name = InventoryUI.NewRect("Name", root);
        name.anchorMin = new Vector2(0f, 0f);
        name.anchorMax = new Vector2(1f, 0f);
        name.pivot = new Vector2(0.5f, 0f);
        name.offsetMin = new Vector2(inset * 2f, inset * 2.2f);
        name.offsetMax = new Vector2(-inset * 2f, inset * 2.2f + nameH);
        view._name = InventoryUI.AddText(name, font, string.Empty, Size(15, scale),
            TextAnchor.LowerCenter, InventoryPalette.TextDim, FontStyle.Normal);

        // "EQUIPPED" ribbon across the top of the cell
        view._ribbon = InventoryUI.NewRect("EquippedRibbon", root);
        view._ribbon.anchorMin = new Vector2(0f, 1f);
        view._ribbon.anchorMax = new Vector2(1f, 1f);
        view._ribbon.pivot = new Vector2(0.5f, 1f);
        view._ribbon.offsetMin = new Vector2(inset, -(inset + ribbonH));
        view._ribbon.offsetMax = new Vector2(-inset, -inset);
        InventoryUI.AddImage(view._ribbon, solidSprite, InventoryPalette.AccentDim,
            Image.Type.Simple, false);

        RectTransform ribbonText = InventoryUI.Stretch(InventoryUI.NewRect("Label", view._ribbon), 0f);
        InventoryUI.AddText(ribbonText, font, InventoryUI.Track("EQUIPPED"), Size(14, scale),
            TextAnchor.MiddleCenter, InventoryPalette.Accent, FontStyle.Bold);
        view._ribbon.gameObject.SetActive(false);

        // small rust diamond in the top-right corner, a second read of "in hand"
        RectTransform corner = InventoryUI.NewRect("Marker", root);
        corner.anchorMin = new Vector2(1f, 1f);
        corner.anchorMax = new Vector2(1f, 1f);
        corner.pivot = new Vector2(1f, 1f);
        corner.anchoredPosition = new Vector2(-inset * 2.4f, -(inset * 2f + ribbonH));
        corner.sizeDelta = new Vector2(11f * scale, 11f * scale);
        corner.localRotation = Quaternion.Euler(0f, 0f, 45f);
        view._corner = InventoryUI.AddImage(corner, solidSprite, InventoryPalette.Accent,
            Image.Type.Simple, false);
        view._corner.enabled = false;

        view.Refresh();
        return view;
    }

    public void SetItem(InventoryItem item)
    {
        _item = item;
        Refresh();
    }

    public void SetSelected(bool value)
    {
        _selected = value;
        Refresh();
    }

    public bool IsEmpty
    {
        get { return _item == null; }
    }

    private void Refresh()
    {
        bool equipped = _item != null && _item.Equipped;

        _ribbon.gameObject.SetActive(equipped);
        _corner.enabled = equipped;
        _number.enabled = !equipped;

        if (_item == null)
        {
            _icon.sprite = null;
            _icon.color = Color.clear;
            _name.text = string.Empty;
        }
        else
        {
            _icon.sprite = _item.Icon;
            _icon.color = _item.Icon != null ? Color.white : Color.clear;
            _name.text = _item.DisplayName;
            _name.color = equipped ? InventoryPalette.Accent : InventoryPalette.TextDim;
        }

        // border: equipped beats selected beats hovered beats resting
        Color edge;
        if (equipped)
        {
            edge = InventoryPalette.SlotEdgeEquipped;
        }
        else if (_selected || _hovered)
        {
            edge = InventoryPalette.SlotEdgeHover;
        }
        else
        {
            edge = InventoryPalette.SlotEdge;
        }

        if (_hovered && !equipped)
        {
            edge = Color.Lerp(edge, InventoryPalette.Accent, 0.35f);
        }

        _frame.color = edge;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        Refresh();
        if (_onHover != null)
        {
            _onHover(_index, true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        Refresh();
        if (_onHover != null)
        {
            _onHover(_index, false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_onClick != null)
        {
            _onClick(_index);
        }
    }

    private static int Size(int baseSize, float scale)
    {
        return Mathf.Max(8, Mathf.RoundToInt(baseSize * scale));
    }
}
