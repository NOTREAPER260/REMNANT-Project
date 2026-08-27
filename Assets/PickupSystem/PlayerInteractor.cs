using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Looks where the player looks, shows a prompt when an <see cref="IInteractable"/>
/// is in reach, and triggers it when the interact key is pressed.
/// Works for pickups, doors, and anything else implementing the interface.
/// </summary>
/// ကင်မရာရှေ့ကို ray တစ်ခုပစ်ပြီး E နှိပ်လို့ရတဲ့အရာ တွေ့ရင် အောက်မှာ စာပြပါတယ်။
/// ပစ္စည်းကောက်တာရော တံခါးဖွင့်တာရော ဒီတစ်ခုတည်းနဲ့ ကိုင်တွယ်ပါတယ်။
/// GameObject တစ်ခုပေါ်မှာ တစ်ခုတည်း တင်ထားရုံနဲ့ ရပါပြီ — camera ကို အလိုအလျောက် ရှာပါတယ်။
[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Leave empty to find the inventory in the scene.")]
    [SerializeField] private HorrorInventory inventory;
    [Tooltip("Leave empty to use Camera.main.")]
    [SerializeField] private Camera eyes;

    [Header("Reach")]
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField, Range(0.5f, 8f)] private float reach = 3f;
    [Tooltip("Forgiveness on aim when the thin ray misses.")]
    [SerializeField, Range(0f, 0.5f)] private float aimAssistRadius = 0.14f;
    [SerializeField] private LayerMask blockingLayers = ~0;

    [Header("Prompt")]
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private float messageDuration = 1.4f;

    private const int MaxHits = 16;

    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
    private IInteractable _target;
    private Transform _ignoreRoot;

    private GameObject _promptRoot;
    private Text _promptText;
    private Image _crosshair;
    private float _messageUntil;

    /// <summary>The inventory this player carries. Pickups read it through here.</summary>
    public HorrorInventory Inventory
    {
        get { return inventory; }
    }

    // ---------------------------------------------------------------- setup

    private void Awake()
    {
        BuildPromptUI();
    }

    private void Start()
    {
        if (inventory == null)
        {
            inventory = Object.FindFirstObjectByType<HorrorInventory>();
        }
    }

    private void BuildPromptUI()
    {
        Font font = InventoryUI.DefaultFont();

        _promptRoot = new GameObject("Interaction Prompt UI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        _promptRoot.transform.SetParent(transform, false);

        Canvas canvas = _promptRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;                       // under the inventory (500)

        CanvasScaler scaler = _promptRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _promptRoot.GetComponent<CanvasGroup>().blocksRaycasts = false;

        RectTransform dot = InventoryUI.NewRect("Crosshair", _promptRoot.transform);
        dot.anchorMin = new Vector2(0.5f, 0.5f);
        dot.anchorMax = new Vector2(0.5f, 0.5f);
        dot.pivot = new Vector2(0.5f, 0.5f);
        dot.anchoredPosition = Vector2.zero;
        dot.sizeDelta = new Vector2(4f, 4f);
        _crosshair = InventoryUI.AddImage(dot, InventoryTextureFactory.CreateSolid(Color.white),
            InventoryPalette.TextFaint, Image.Type.Simple, false);
        _crosshair.enabled = showCrosshair;

        RectTransform prompt = InventoryUI.NewRect("Prompt", _promptRoot.transform);
        prompt.anchorMin = new Vector2(0.5f, 0.5f);
        prompt.anchorMax = new Vector2(0.5f, 0.5f);
        prompt.pivot = new Vector2(0.5f, 0.5f);
        prompt.anchoredPosition = new Vector2(0f, -130f);
        prompt.sizeDelta = new Vector2(1100f, 34f);
        _promptText = InventoryUI.AddText(prompt, font, string.Empty, 20,
            TextAnchor.MiddleCenter, InventoryPalette.TextPrimary, FontStyle.Normal);
    }

    // -------------------------------------------------------------- runtime

    private void Update()
    {
        // The inventory owns the screen while it is open.
        if (inventory != null && inventory.IsOpen)
        {
            _target = null;
            Show(string.Empty, false);
            return;
        }

        if (!EnsureEyes())
        {
            return;
        }

        _target = FindTarget();

        Keyboard keyboard = Keyboard.current;
        if (_target != null && keyboard != null && keyboard[interactKey].wasPressedThisFrame)
        {
            string message = _target.Interact(gameObject);
            if (!string.IsNullOrEmpty(message))
            {
                Flash(message);
            }
            return;
        }

        if (Time.time < _messageUntil)
        {
            return;                                       // a flash message is on screen
        }

        if (_target != null)
        {
            Show("[ " + interactKey.ToString().ToUpperInvariant() + " ]   "
                 + InventoryUI.Track(_target.Prompt), true);
        }
        else
        {
            Show(string.Empty, false);
        }
    }

    /// <summary>
    /// Resolve the aiming camera, and with it the player root whose own colliders
    /// must never count as obstacles. Works whether `eyes` was assigned or not.
    /// </summary>
    private bool EnsureEyes()
    {
        if (eyes == null)
        {
            eyes = Camera.main;
        }

        if (eyes == null)
        {
            return false;
        }

        if (_ignoreRoot == null)
        {
            _ignoreRoot = eyes.transform.root;
        }

        return true;
    }

    /// <summary>
    /// Nearest interactable the player can actually see. Anything solid in the way
    /// blocks it, so doors cannot be opened and items cannot be grabbed through walls.
    /// </summary>
    /// အနီးဆုံး E နှိပ်လို့ရတဲ့အရာကို ရှာတာပါ။ ကြားထဲမှာ နံရံခံနေရင် မရပါဘူး။
    private IInteractable FindTarget()
    {
        Ray ray = new Ray(eyes.transform.position, eyes.transform.forward);

        int count = Physics.RaycastNonAlloc(ray, _hits, reach, blockingLayers,
            QueryTriggerInteraction.Ignore);
        IInteractable direct = NearestInteractable(count, true);
        if (direct != null)
        {
            return direct;
        }

        if (aimAssistRadius <= 0f)
        {
            return null;
        }

        // Thin ray missed - sweep a small sphere so near-misses still count.
        count = Physics.SphereCastNonAlloc(ray, aimAssistRadius, _hits, reach, blockingLayers,
            QueryTriggerInteraction.Ignore);
        return NearestInteractable(count, false);
    }

    /// <param name="stopAtBlocker">
    /// True for the precise ray: the first solid thing that is not interactable hides
    /// whatever is behind it. False for aim assist, which only looks for interactables.
    /// </param>
    private IInteractable NearestInteractable(int count, bool stopAtBlocker)
    {
        int remaining = count;
        float lastDistance = -1f;

        while (remaining > 0)
        {
            // Pull hits out in distance order without allocating a sorted copy.
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance <= lastDistance)
                {
                    continue;
                }
                if (best < 0 || _hits[i].distance < _hits[best].distance)
                {
                    best = i;
                }
            }

            if (best < 0)
            {
                return null;
            }

            lastDistance = _hits[best].distance;
            remaining--;

            Collider collider = _hits[best].collider;
            if (collider == null)
            {
                continue;
            }

            // The player's own capsule is not an obstacle.
            if (_ignoreRoot != null && collider.transform.IsChildOf(_ignoreRoot))
            {
                continue;
            }

            IInteractable interactable = collider.GetComponentInParent<IInteractable>();
            if (interactable != null && interactable.CanInteract)
            {
                return interactable;
            }

            if (stopAtBlocker)
            {
                return null;
            }
        }

        return null;
    }

    private void Flash(string message)
    {
        _messageUntil = Time.time + messageDuration;
        Show(message, true);
    }

    private void Show(string message, bool highlight)
    {
        if (_promptText != null)
        {
            _promptText.text = message;
        }

        if (_crosshair != null)
        {
            _crosshair.enabled = showCrosshair;
            _crosshair.color = highlight ? InventoryPalette.Accent : InventoryPalette.TextFaint;
        }
    }
}
