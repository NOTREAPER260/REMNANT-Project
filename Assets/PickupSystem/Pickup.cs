using UnityEngine;

/// <summary>
/// Drop this on ANY object to make it collectable. The inventory icon is
/// rendered from the object itself, so it works on a cube, a prop or a whole
/// prefab without drawing anything by hand.
/// </summary>
/// ဘယ် object ပေါ်မှာမဆို တင်လိုက်ရုံနဲ့ ကောက်ယူလို့ရသွားပါပြီ။
/// Inventory ထဲက ပုံကို object ကိုယ်တိုင်က render လုပ်ပေးလို့ icon ဆွဲစရာ မလိုပါဘူး။
///
/// အသုံးပြုပုံ:
///   1. Object ကို ရွေး → Add Component → Pickup
///   2. Item Name / Description ဖြည့် (နာမည်က object နာမည်အတိုင်း အလိုလိုဝင်ပါတယ်)
///   3. Icon Rotation နဲ့ ပုံရဲ့ ထောင့်ကို ချိန်လို့ရပါတယ်
[DisallowMultipleComponent]
public class Pickup : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [Tooltip("Shown in the inventory slot. Keep it English - uGUI cannot shape Burmese.")]
    [SerializeField] private string itemName = "ITEM";
    [SerializeField, TextArea(2, 4)] private string description = "";

    [Header("Inventory Icon")]
    [Tooltip("Leave empty to photograph this object. Assign a Sprite to override.")]
    [SerializeField] private Sprite iconOverride;
    [SerializeField, Range(64, 512)] private int iconSize = 192;
    [Tooltip("Pose the object is photographed at. A three-quarter angle reads best.")]
    [SerializeField] private Vector3 iconRotation = new Vector3(18f, -28f, 0f);
    [Tooltip("Empty margin around the object. 1 = tight crop.")]
    [SerializeField, Range(1f, 2f)] private float iconPadding = 1.18f;

    [Header("Held In Hand")]
    [Tooltip("Already in the player's hand at the start. It goes straight into the " +
             "inventory, stays visible, and returns to the hand when picked up again.")]
    [SerializeField] private bool heldInHand;


    [Header("On Pickup")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.7f;

    private Sprite _cachedIcon;

    // Where a hand-held object lives when carried, so it can go back there.
    private Transform _homeParent;
    private Vector3 _homeLocalPosition;
    private Quaternion _homeLocalRotation;
    private bool _homeRecorded;

    /// လက်ထဲမှာ ကိုင်ထားတဲ့ ပစ္စည်းလား။
    public bool HeldInHand
    {
        get { return heldInHand; }
    }

    public string ItemName
    {
        get { return string.IsNullOrEmpty(itemName) ? name.ToUpperInvariant() : itemName; }
    }

    public string Description
    {
        get { return description; }
    }


    // --- IInteractable ---------------------------------------------------

    public string Prompt { get { return "PICK UP   " + ItemName; } }

    public bool CanInteract { get { return true; } }

    /// <summary>Take the item, using the inventory the interactor is wired to.</summary>
    /// ကောက်ယူတာပါ။ Inventory ကို PlayerInteractor ဆီကနေ ယူသုံးလို့
    /// Pickup တစ်ခုချင်းစီမှာ ချိတ်ပေးစရာ မလိုပါဘူး။
    public string Interact(GameObject interactor)
    {
        HorrorInventory inventory = null;

        if (interactor != null)
        {
            PlayerInteractor player = interactor.GetComponentInParent<PlayerInteractor>();
            if (player != null)
            {
                inventory = player.Inventory;
            }
        }

        if (inventory == null)
        {
            inventory = Object.FindFirstObjectByType<HorrorInventory>();
        }

        if (inventory == null)
        {
            return "NO INVENTORY IN SCENE";
        }

        // Photograph the object before it leaves the world.
        InventoryItem item = CreateItem();

        if (!inventory.TryAddItem(item))
        {
            return "INVENTORY FULL";
        }

        string name = ItemName;
        OnPickedUp();
        return name + "   ACQUIRED";
    }

    /// Called by Unity the first time the component is added, and on "Reset".
    /// Component ထည့်လိုက်တာနဲ့ Unity က ဒါကို ခေါ်ပေးလို့ အလိုအလျောက် ပြင်ဆင်ပေးပါတယ်။
    private void Awake()
    {
        RecordHome();

        // A tool riding on the camera must not shove the player around, and it
        // must not sit in front of the aim ray either.
        if (heldInHand)
        {
            SetCollidersEnabled(false);
        }
    }

    /// <summary>
    /// The aim ray can only find this object while its colliders are on, so they
    /// follow the object: off in the hand, on once it is lying in the world.
    /// </summary>
    /// လက်ထဲမှာ ကိုင်ထားစဉ် collider ပိတ်၊ မြေပြင်ပေါ် ချလိုက်ရင် ဖွင့်ပေးတာပါ။
    /// မဖွင့်ရင် E ရဲ့ ray က ထိစရာမရှိလို့ ဘယ်တော့မှ ပြန်ကောက်လို့ ရမှာ မဟုတ်ပါဘူး။
    private void SetCollidersEnabled(bool value)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = value;
        }
    }

    /// <summary>Remember the hand pose before anything moves the object.</summary>
    private void RecordHome()
    {
        if (_homeRecorded)
        {
            return;
        }

        _homeParent = transform.parent;
        _homeLocalPosition = transform.localPosition;
        _homeLocalRotation = transform.localRotation;
        _homeRecorded = true;
    }


    /// Called by Unity the first time the component is added, and on "Reset".
    /// Component ထည့်လိုက်တာနဲ့ Unity က ဒါကို ခေါ်ပေးလို့ အလိုအလျောက် ပြင်ဆင်ပေးပါတယ်။
    private void Reset()
    {
        itemName = name.ToUpperInvariant();

        // Without a collider the player's aim ray has nothing to hit.
        if (GetComponentInChildren<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    /// <summary>
    /// Build the inventory entry for this object, rendering the icon the first
    /// time it is asked for and reusing it afterwards.
    /// </summary>
    public InventoryItem CreateItem()
    {
        Sprite icon = iconOverride;

        if (icon == null)
        {
            if (_cachedIcon == null)
            {
                _cachedIcon = ObjectPreviewRenderer.Render(gameObject, iconSize, iconRotation, iconPadding);
            }
            icon = _cachedIcon;
        }

        InventoryItem item = new InventoryItem(ItemName, description, icon, heldInHand);

        // Picked-up objects are hidden, never destroyed, so every one of them
        // can always be dropped again.
        item.WorldObject = gameObject;
        item.Droppable = true;
        return item;
    }

    /// <summary>Take the object out of the world once it is safely in the bag.</summary>
    /// ဖျက်တာ မဟုတ်ပါဘူး — ဝှက်ထားရုံပါ။ ဒါမှ ဘယ်အချိန်မဆို ပြန်ချလို့ရမှာပါ။
    public void OnPickedUp()
    {
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        }

        RecordHome();

        if (heldInHand && _homeParent != null)
        {
            // A carried tool goes back to the hand and stays visible. The aim ray
            // ignores it because it is a child of the player.
            transform.SetParent(_homeParent, false);
            transform.localPosition = _homeLocalPosition;
            transform.localRotation = _homeLocalRotation;
            SetCollidersEnabled(false);
            gameObject.SetActive(true);
            return;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Put the object back into the world at <paramref name="position"/>.
    /// Any rigidbody is woken up and zeroed so it falls naturally from there.
    /// </summary>
    /// Inventory ကနေ ပြန်ချတဲ့အခါ ခေါ်တာပါ။ Rigidbody ပါရင် အရှိန်ကို သုညပြန်ချပေးလို့
    /// ကောက်ခင်က အရှိန်အဟုန်တွေ ကျန်မနေတော့ပါဘူး။
    public void Restore(Vector3 position, Quaternion rotation)
    {
        RecordHome();

        // Detach first, or a dropped item would still ride along with the camera.
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);
        SetCollidersEnabled(true);

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }
    }
}
