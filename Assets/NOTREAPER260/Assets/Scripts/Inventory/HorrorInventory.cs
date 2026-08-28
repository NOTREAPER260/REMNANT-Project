using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Tab-toggled survival-horror inventory. Builds its own Canvas, EventSystem
/// and sprites at runtime, so it needs no prefab, no imported art and no
/// TextMeshPro Essentials.
/// </summary>
/// Tab နှိပ်ရင် ပွင့်တဲ့ horror inventory ပါ။ Canvas, EventSystem, texture
/// အားလုံးကို runtime မှာ ကိုယ်တိုင်ဆောက်လို့ scene ထဲမှာ ဘာမှ ကြိုပြင်ဆင်စရာ မလိုပါဘူး။
/// GameObject တစ်ခုပေါ်မှာ ဒီ script တစ်ခုတည်း တင်လိုက်ရုံပါပဲ။
[DisallowMultipleComponent]
public class HorrorInventory : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField, Range(1, 24)] private int slotCount = 10;
    [SerializeField, Range(1, 8)] private int columns = 5;

    [Header("Starting Gear")]
    [Tooltip("The flashlight the player already carries. Leave empty to find it by name.")]
    [SerializeField] private Light flashlightLight;
    [Tooltip("Fallback lookup when no Light is assigned above.")]
    [SerializeField] private string flashlightObjectName = "Spot Light";

    [Header("Layout")]
    [Tooltip("Scales the whole panel and every label at once. 1 = the built-in size.")]
    [SerializeField, Range(0.6f, 2f)] private float uiScale = 1f;

    [Header("Input")]
    [SerializeField] private Key toggleKey = Key.Tab;
    [Tooltip("Drops the selected item back into the world.")]
    [SerializeField] private Key dropKey = Key.G;
    [SerializeField] private bool closeOnEscape = true;

    [Header("Dropping")]
    [Tooltip("How far in front of the camera a dropped item lands.")]
    [SerializeField, Range(0.2f, 4f)] private float dropDistance = 1.1f;

    [Header("Feel")]
    [Tooltip("Disable the FirstPersonController while the inventory is open.")]
    [SerializeField] private bool freezePlayerWhileOpen = true;
    [SerializeField, Range(0f, 0.25f)] private float panelFlicker = 0.055f;
    [SerializeField, Range(0.02f, 0.6f)] private float fadeDuration = 0.12f;

    // --- layout, all multiplied by uiScale --------------------------------
    private const float BaseCell = 184f;
    private const float BaseSpacing = 18f;
    private const float BasePadX = 54f;
    private const float BaseHeader = 122f;
    private const float BaseDetail = 176f;

    private float Cell { get { return BaseCell * uiScale; } }
    private float Spacing { get { return BaseSpacing * uiScale; } }
    private float PadX { get { return BasePadX * uiScale; } }
    private float HeaderHeight { get { return BaseHeader * uiScale; } }
    private float DetailHeight { get { return BaseDetail * uiScale; } }

    /// Font sizes go through here so one slider resizes every label.
    private int F(int baseSize)
    {
        return Mathf.Max(8, Mathf.RoundToInt(baseSize * uiScale));
    }

    // --- runtime state ----------------------------------------------------
    private InventoryItem[] _items;
    private readonly List<InventorySlotView> _slots = new List<InventorySlotView>();

    private GameObject _root;
    private CanvasGroup _rootGroup;
    private CanvasGroup _panelGroup;
    private Text _statusText;
    private Text _detailName;
    private Text _detailBody;

    private bool _open;
    private int _selected = -1;
    private int _hovered = -1;
    private float _noiseSeed;
    private int _flashlightSlot = -1;
    private bool _lastBeamOn;
    private float _flashUntil;

    private FirstPersonController _controller;
    private StarterAssetsInputs _playerInputs;

    public bool IsOpen { get { return _open; } }
    public int SlotCount { get { return _items != null ? _items.Length : slotCount; } }

    // ---------------------------------------------------------------- setup

    private void Awake()
    {
        _noiseSeed = Random.value * 100f;
        _items = new InventoryItem[Mathf.Max(1, slotCount)];

        EnsureEventSystem();
        BuildUI();

        _root.SetActive(false);
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable = false;
    }

    private void Start()
    {
        _controller = Object.FindFirstObjectByType<FirstPersonController>();
        if (_controller != null)
        {
            _playerInputs = _controller.GetComponent<StarterAssetsInputs>();
        }

        AddStartingFlashlight();
        AddHandHeldPickups();
        RefreshSlots();
    }

    /// ကစားသမား လက်ထဲမှာ ရှိပြီးသား ဓာတ်မီးကို slot ပထမဆုံးထဲ ထည့်ပြီး equipped မှတ်ပါတယ်။
    private void AddStartingFlashlight()
    {
        if (flashlightLight == null && !string.IsNullOrEmpty(flashlightObjectName))
        {
            GameObject found = GameObject.Find(flashlightObjectName);
            if (found != null)
            {
                flashlightLight = found.GetComponent<Light>();
            }
        }

        Sprite icon = InventoryTextureFactory.CreateFlashlightIcon(
            128, InventoryPalette.IconBody, InventoryPalette.IconLens, InventoryPalette.IconBeam);

        InventoryItem flashlight = new InventoryItem(
            "FLASHLIGHT",
            "Held in the right hand. Press F to switch the beam. The bulb is failing.",
            icon,
            true);

        // The one permanent item: no world object to put back, and never droppable.
        // ဓာတ်မီးကတော့ ဘယ်တော့မှ မချရပါဘူး — ချစရာ world object ကို မထားပါဘူး။
        flashlight.WorldObject = null;
        flashlight.Droppable = false;

        _items[0] = flashlight;
        _flashlightSlot = 0;
        _lastBeamOn = flashlightLight != null && flashlightLight.enabled;
    }

/// <summary>
    /// Anything already in the player's hand (a crowbar, a tool) gets a slot at
    /// start. Unlike the flashlight these are real world objects, so they can be
    /// dropped with the drop key and picked back up again.
    /// </summary>
    /// လက်ထဲမှာ ကိုင်ထားပြီးသား ပစ္စည်းတွေကို စတင်ချိန်မှာ slot ထဲ ထည့်ပေးတာပါ။
    /// ဓာတ်မီးနဲ့ မတူတာက ဒါတွေက တကယ့် object တွေမို့ ချလို့ရ၊ ပြန်ကောက်လို့ရပါတယ်။
    private void AddHandHeldPickups()
    {
        Pickup[] pickups = Object.FindObjectsByType<Pickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < pickups.Length; i++)
        {
            if (!pickups[i].HeldInHand)
            {
                continue;
            }

            if (!TryAddItem(pickups[i].CreateItem()))
            {
                Debug.LogWarning("[Inventory] No room for '" + pickups[i].ItemName + "'.", pickups[i]);
            }
        }
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        // Project runs on the new Input System only, so the legacy
        // StandaloneInputModule would throw. Use the Input System module.
        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.transform.SetParent(transform, false);
    }

    // ------------------------------------------------------------------- UI

    private void BuildUI()
    {
        Font font = InventoryUI.DefaultFont();

        int rows = Mathf.CeilToInt(_items.Length / (float)Mathf.Max(1, columns));
        float gridW = columns * Cell + (columns - 1) * Spacing;
        float gridH = rows * Cell + (rows - 1) * Spacing;
        float panelW = gridW + 2f * PadX;
        float panelH = HeaderHeight + gridH + DetailHeight;

        Sprite panelSprite = InventoryTextureFactory.CreateFrame(
            64, InventoryPalette.PanelFill, InventoryPalette.PanelEdge, 2, 10);
        Sprite slotSprite = InventoryTextureFactory.CreateFrame(
            48, InventoryPalette.SlotFill, Color.white, 2, 7);
        Sprite solid = InventoryTextureFactory.CreateSolid(Color.white);
        Sprite grain = InventoryTextureFactory.CreateGrain(128, 0.16f, 8675309);
        Sprite vignette = InventoryTextureFactory.CreateVignette(256, 0.25f, 1f, 0.85f);

        // --- canvas -------------------------------------------------------
        _root = new GameObject("Horror Inventory UI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        _root.transform.SetParent(transform, false);

        Canvas canvas = _root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _rootGroup = _root.GetComponent<CanvasGroup>();

        // --- backdrop + vignette -----------------------------------------
        RectTransform backdrop = InventoryUI.Stretch(
            InventoryUI.NewRect("Backdrop", _root.transform), 0f);
        InventoryUI.AddImage(backdrop, solid, InventoryPalette.ScreenDim, Image.Type.Simple, true);

        RectTransform vig = InventoryUI.Stretch(
            InventoryUI.NewRect("Vignette", _root.transform), 0f);
        InventoryUI.AddImage(vig, vignette, Color.white, Image.Type.Simple, false);

        // --- panel --------------------------------------------------------
        RectTransform panel = InventoryUI.NewRect("Panel", _root.transform);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(panelW, panelH);
        InventoryUI.AddImage(panel, panelSprite, Color.white, Image.Type.Sliced, true);
        _panelGroup = panel.gameObject.AddComponent<CanvasGroup>();

        RectTransform grainRect = InventoryUI.Stretch(
            InventoryUI.NewRect("Grain", panel), 3f);
        InventoryUI.AddImage(grainRect, grain, Color.white, Image.Type.Tiled, false);

        // --- header -------------------------------------------------------
        RectTransform title = TopLeft(panel, "Title",
            new Vector2(PadX, -34f * uiScale), new Vector2(640f * uiScale, 44f * uiScale));
        InventoryUI.AddText(title, font, InventoryUI.Track("INVENTORY"), F(32),
            TextAnchor.UpperLeft, InventoryPalette.TextPrimary, FontStyle.Bold);

        RectTransform status = InventoryUI.NewRect("Status", panel);
        status.anchorMin = new Vector2(1f, 1f);
        status.anchorMax = new Vector2(1f, 1f);
        status.pivot = new Vector2(1f, 1f);
        status.anchoredPosition = new Vector2(-PadX, -40f * uiScale);
        status.sizeDelta = new Vector2(400f * uiScale, 26f * uiScale);
        _statusText = InventoryUI.AddText(status, font, string.Empty, F(16),
            TextAnchor.UpperRight, InventoryPalette.TextDim, FontStyle.Normal);

        AddRule(panel, solid, -100f * uiScale);

        // --- slot grid ----------------------------------------------------
        RectTransform grid = InventoryUI.NewRect("Grid", panel);
        grid.anchorMin = new Vector2(0.5f, 1f);
        grid.anchorMax = new Vector2(0.5f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = new Vector2(0f, -HeaderHeight);
        grid.sizeDelta = new Vector2(gridW, gridH);

        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(Cell, Cell);
        layout.spacing = new Vector2(Spacing, Spacing);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = Mathf.Max(1, columns);
        layout.childAlignment = TextAnchor.UpperCenter;

        for (int i = 0; i < _items.Length; i++)
        {
            InventorySlotView view = InventorySlotView.Build(
                grid, i, font, slotSprite, solid, Cell, uiScale,
                OnSlotHover, OnSlotClick);
            _slots.Add(view);
        }

        // --- detail strip -------------------------------------------------
        float detailTop = HeaderHeight + gridH + 28f * uiScale;
        AddRule(panel, solid, -detailTop);

        RectTransform detailName = TopLeft(panel, "DetailName",
            new Vector2(PadX, -(detailTop + 20f * uiScale)),
            new Vector2(panelW - 2f * PadX, 30f * uiScale));
        _detailName = InventoryUI.AddText(detailName, font, string.Empty, F(21),
            TextAnchor.UpperLeft, InventoryPalette.Accent, FontStyle.Bold);

        RectTransform detailBody = TopLeft(panel, "DetailBody",
            new Vector2(PadX, -(detailTop + 56f * uiScale)),
            new Vector2(panelW - 2f * PadX, 56f * uiScale));
        _detailBody = InventoryUI.AddText(detailBody, font, string.Empty, F(16),
            TextAnchor.UpperLeft, InventoryPalette.TextDim, FontStyle.Normal);
        _detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;

        RectTransform hint = InventoryUI.NewRect("Hint", panel);
        hint.anchorMin = new Vector2(1f, 0f);
        hint.anchorMax = new Vector2(1f, 0f);
        hint.pivot = new Vector2(1f, 0f);
        hint.anchoredPosition = new Vector2(-PadX, 24f * uiScale);
        hint.sizeDelta = new Vector2(760f * uiScale, 24f * uiScale);
        InventoryUI.AddText(hint, font,
            InventoryUI.Track(dropKey.ToString().ToUpperInvariant()) + " DROP     "
            + InventoryUI.Track("TAB") + " / " + InventoryUI.Track("ESC") + " CLOSE",
            F(15), TextAnchor.LowerRight, InventoryPalette.TextFaint, FontStyle.Normal);
    }

    private static RectTransform TopLeft(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        RectTransform rect = InventoryUI.NewRect(name, parent);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void AddRule(RectTransform panel, Sprite solid, float y)
    {
        RectTransform rule = InventoryUI.NewRect("Rule", panel);
        rule.anchorMin = new Vector2(0f, 1f);
        rule.anchorMax = new Vector2(1f, 1f);
        rule.pivot = new Vector2(0.5f, 1f);
        rule.offsetMin = new Vector2(PadX, y - 1f);
        rule.offsetMax = new Vector2(-PadX, y);
        InventoryUI.AddImage(rule, solid, InventoryPalette.AccentDim, Image.Type.Simple, false);
    }

    // -------------------------------------------------------------- runtime

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard[toggleKey].wasPressedThisFrame)
            {
                Toggle();
            }
            else if (_open && closeOnEscape && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
            else if (_open && keyboard[dropKey].wasPressedThisFrame)
            {
                DropItem(_hovered >= 0 ? _hovered : _selected);
            }
        }

        float target = _open ? 1f : 0f;
        if (!Mathf.Approximately(_rootGroup.alpha, target))
        {
            float step = Time.unscaledDeltaTime / Mathf.Max(fadeDuration, 0.01f);
            _rootGroup.alpha = Mathf.MoveTowards(_rootGroup.alpha, target, step);

            if (!_open && _rootGroup.alpha <= 0.001f)
            {
                _root.SetActive(false);
            }
        }

        // Beam state can change while the panel is open (F still works), but only
        // rebuild the detail line when it actually flips.
        if (_open && flashlightLight != null && flashlightLight.enabled != _lastBeamOn)
        {
            _lastBeamOn = flashlightLight.enabled;
            UpdateDetail(_hovered >= 0 ? _hovered : _selected);
        }

        if (_open && panelFlicker > 0f)
        {
            // ဓာတ်မီးလိုပဲ UI ကိုလည်း အနည်းငယ် တဝင်းဝင်းဖြစ်စေတယ်။
            float n = Mathf.PerlinNoise(_noiseSeed, Time.unscaledTime * 7f);
            _panelGroup.alpha = 1f - panelFlicker * n;
        }
    }

    public void Toggle()
    {
        if (_open)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (_open)
        {
            return;
        }

        _open = true;
        _root.SetActive(true);
        _rootGroup.blocksRaycasts = true;
        _rootGroup.interactable = true;
        _panelGroup.alpha = 1f;

        SetPlayerControl(false);
        SetSelected(_selected >= 0 ? _selected : FirstOccupiedSlot());
        RefreshSlots();
    }

    public void Close()
    {
        if (!_open)
        {
            return;
        }

        _open = false;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable = false;
        _hovered = -1;

        SetPlayerControl(true);
    }

    /// <summary>Freeze look and movement, and hand the mouse back to the player.</summary>
    /// Inventory ဖွင့်ထားစဉ် ကင်မရာလှည့်တာ/လမ်းလျှောက်တာကို ရပ်ပြီး mouse ကို ပြန်ပေးပါတယ်။
    private void SetPlayerControl(bool restore)
    {
        if (_controller != null && freezePlayerWhileOpen)
        {
            _controller.enabled = restore;
        }

        if (_playerInputs != null)
        {
            _playerInputs.cursorInputForLook = restore;
            _playerInputs.cursorLocked = restore;

            if (!restore)
            {
                _playerInputs.move = Vector2.zero;
                _playerInputs.look = Vector2.zero;
                _playerInputs.sprint = false;
                _playerInputs.jump = false;
            }
        }

        Cursor.lockState = restore ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !restore;
    }

    // ----------------------------------------------------------- slot logic

    /// <summary>Drop an item into the first free slot. False when the bag is full.</summary>
    public bool TryAddItem(InventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                _items[i] = item;
                RefreshSlots();
                return true;
            }
        }

        return false;
    }

    public bool RemoveItem(int index)
    {
        if (index < 0 || index >= _items.Length || _items[index] == null)
        {
            return false;
        }

        _items[index] = null;
        RefreshSlots();
        return true;
    }

    /// <summary>Mark one slot as held; every other slot is cleared of the mark.</summary>
    public void SetEquipped(int index)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null)
            {
                _items[i].Equipped = i == index;
            }
        }

        RefreshSlots();
    }

    /// <summary>
    /// Put one slot's item back into the world, in front of the player and
    /// settled on whatever surface is under that spot. The equipped flashlight
    /// is not droppable, so it stays put.
    /// </summary>
    /// Slot တစ်ခုထဲက ပစ္စည်းကို ကစားသမားရှေ့မှာ ပြန်ချပေးတာပါ။ ကြမ်းပြင်ကို ray ပစ်ပြီး
    /// လေထဲ မပေါ်နေအောင် ချထားပါတယ်။ ကိုင်ထားတဲ့ ဓာတ်မီးကတော့ ချလို့မရပါဘူး။
    public bool DropItem(int index)
    {
        if (index < 0 || index >= _items.Length)
        {
            Flash("SELECT A SLOT FIRST");
            return false;
        }

        if (_items[index] == null)
        {
            Flash("THAT SLOT IS EMPTY");
            return false;
        }

        InventoryItem item = _items[index];
        if (!item.Droppable)
        {
            // Only the flashlight is permanent; everything picked up can go back.
            Flash(index == _flashlightSlot
                ? InventoryUI.Track(item.DisplayName) + "   CAN NEVER BE DROPPED"
                : "THIS CAN NEVER BE DROPPED");
            return false;
        }

        GameObject go = item.WorldObject;
        Camera eyes = Camera.main;
        if (go == null || eyes == null)
        {
            Flash("NOWHERE TO DROP IT");
            return false;
        }

        Vector3 position = FindDropSpot(eyes, go);

        Pickup pickup = go.GetComponent<Pickup>();
        if (pickup != null)
        {
            pickup.Restore(position, go.transform.rotation);
        }
        else
        {
            go.transform.position = position;
            go.SetActive(true);
        }

        _items[index] = null;
        if (_selected == index)
        {
            _selected = -1;
        }

        RefreshSlots();
        Flash(InventoryUI.Track(item.DisplayName) + "   DROPPED");
        return true;
    }

    /// <summary>
    /// A clear spot ahead of the camera: stop short of any wall, then settle
    /// onto the ground so the item never lands inside geometry or in mid-air.
    /// </summary>
    private Vector3 FindDropSpot(Camera eyes, GameObject go)
    {
        Vector3 origin = eyes.transform.position;
        Vector3 forward = eyes.transform.forward;

        float distance = dropDistance;
        RaycastHit ahead;
        if (Physics.Raycast(origin, forward, out ahead, dropDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(0.15f, ahead.distance - 0.2f);
        }

        Vector3 spot = origin + forward * distance;

        // The object is still disabled here, so it cannot block its own raycast.
        float halfHeight = 0f;
        Renderer renderer = go.GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            halfHeight = renderer.bounds.extents.y;
        }

        RaycastHit ground;
        if (Physics.Raycast(spot + Vector3.up * 0.4f, Vector3.down, out ground, 5f, ~0,
                QueryTriggerInteraction.Ignore))
        {
            spot = ground.point + Vector3.up * (halfHeight + 0.005f);
        }

        return spot;
    }

    /// Briefly replace the detail line with a message.
    private void Flash(string message)
    {
        if (_detailName != null)
        {
            _detailName.text = message;
            _detailName.color = InventoryPalette.Accent;
        }
        if (_detailBody != null)
        {
            _detailBody.text = string.Empty;
        }
        _flashUntil = Time.unscaledTime + 1.2f;
    }

    private void RefreshSlots()
    {
        int carried = 0;
        for (int i = 0; i < _slots.Count && i < _items.Length; i++)
        {
            _slots[i].SetItem(_items[i]);
            _slots[i].SetSelected(i == _selected);

            if (_items[i] != null)
            {
                carried++;
            }
        }

        if (_statusText != null)
        {
            _statusText.text = carried + " / " + _items.Length + "   CARRIED";
        }

        UpdateDetail(_hovered >= 0 ? _hovered : _selected);
    }

    private int FirstOccupiedSlot()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null)
            {
                return i;
            }
        }
        return 0;
    }

    private void SetSelected(int index)
    {
        _selected = index;
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].SetSelected(i == _selected);
        }
        UpdateDetail(_hovered >= 0 ? _hovered : _selected);
    }

    private void OnSlotHover(int index, bool entered)
    {
        if (entered)
        {
            _hovered = index;
        }
        else if (_hovered == index)
        {
            _hovered = -1;
        }

        UpdateDetail(_hovered >= 0 ? _hovered : _selected);
    }

    private void OnSlotClick(int index)
    {
        SetSelected(index);
    }

    private void UpdateDetail(int index)
    {
        if (_detailName == null || _detailBody == null)
        {
            return;
        }

        // Let a "DROPPED" / "CANNOT DROP" message stay readable for a moment.
        if (Time.unscaledTime < _flashUntil)
        {
            return;
        }

        if (index < 0 || index >= _items.Length || _items[index] == null)
        {
            _detailName.text = InventoryUI.Track("EMPTY");
            _detailName.color = InventoryPalette.TextFaint;
            _detailBody.text = "Nothing here.";
            return;
        }

        InventoryItem item = _items[index];
        _detailName.text = InventoryUI.Track(item.DisplayName) + (item.Equipped ? "   [ IN HAND ]" : string.Empty);
        _detailName.color = InventoryPalette.Accent;

        string body = item.Description;
        if (index == _flashlightSlot && flashlightLight != null)
        {
            body += flashlightLight.enabled ? "\nBEAM: ON" : "\nBEAM: OFF";
        }

        // Only advertise the key on things that can actually be put down.
        if (item.Droppable)
        {
            body += "\n[ " + dropKey.ToString().ToUpperInvariant() + " ]  drop";
        }
        else
        {
            body += "\nTHIS CAN NEVER BE DROPPED.";
        }

        _detailBody.text = body;
    }
}
