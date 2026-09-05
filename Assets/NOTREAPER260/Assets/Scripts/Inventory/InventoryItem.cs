using UnityEngine;

/// <summary>
/// One thing that can sit in an inventory slot.
/// All display strings stay in English - Unity's uGUI text cannot shape Burmese.
/// </summary>
/// Inventory slot တစ်ခုထဲမှာ ရှိနိုင်တဲ့ ပစ္စည်းတစ်ခု။
/// စာသားတွေကို အင်္ဂလိပ်လိုပဲ ထားရပါတယ် — uGUI က မြန်မာစာကို မှန်မှန်ကန်ကန် မဖော်ပြနိုင်လို့ပါ။
[System.Serializable]
public class InventoryItem
{
    [SerializeField] private string displayName = "ITEM";
    [SerializeField] private string description = "";
    [SerializeField] private bool equipped;

    private Sprite _icon;
    private GameObject _worldObject;
    private bool _droppable;
    private System.Action _onUse;

    public InventoryItem(string displayName, string description, Sprite icon, bool equipped)
    {
        this.displayName = displayName;
        this.description = description;
        this.equipped = equipped;
        _icon = icon;
    }

    /// <summary>
    /// The hidden scene object this item came from. Dropping re-enables it in
    /// front of the player, so the dropped thing is the exact object that was
    /// picked up - same mesh, same materials, same children.
    /// </summary>
    /// ကောက်ယူထားတဲ့ တကယ့် object ကို ဖျက်မပစ်ဘဲ ဝှက်ထားတာပါ။
    /// ပြန်ချတဲ့အခါ အဲဒီ object ကိုပဲ ပြန်ဖွင့်ပေးလို့ အတိအကျ တူညီပါတယ်။
    public GameObject WorldObject
    {
        get { return _worldObject; }
        set { _worldObject = value; }
    }

    /// ပြန်ချလို့ရလား။ ဓာတ်မီးလို ကိုယ်ပိုင်ပစ္စည်းတွေမှာ false ထားပါတယ်။
    public bool Droppable
    {
        get { return _droppable && _worldObject != null; }
        set { _droppable = value; }
    }

    public string DisplayName
    {
        get { return displayName; }
        set { displayName = value; }
    }

    public string Description
    {
        get { return description; }
        set { description = value; }
    }

    /// ကိုင်ထားလား (equipped) — မှန်ရင် slot ပေါ်မှာ အမှတ်အသား ပြပါတယ်။
    public bool Equipped
    {
        get { return equipped; }
        set { equipped = value; }
    }

    public Sprite Icon
    {
        get { return _icon; }
        set { _icon = value; }
    }

    /// ရွေးထားစဉ် Use key နှိပ်ရင် ခေါ်မယ့် callback. null ဆိုရင် hint လုံးဝ မပြပါ.
    public System.Action OnUse
    {
        get { return _onUse; }
        set { _onUse = value; }
    }
}
