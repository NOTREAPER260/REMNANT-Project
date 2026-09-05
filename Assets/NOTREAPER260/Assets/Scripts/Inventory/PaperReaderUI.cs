using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Full-screen popup that shows one ReadablePaper's text. The sheet is a single
/// page of the same OpenBook model the book reader uses, framed close, so a
/// note found in the world and a page in the book are made of the same paper.
/// Escape files it into the book.
/// </summary>
/// ReadablePaper တစ်ခုရဲ့ စာသားကို full-screen ပြပေးတဲ့ popup ပါ. စာရွက်က
/// BookReaderUI သုံးတဲ့ OpenBook model ရဲ့ စာမျက်နှာတစ်ခုကိုပဲ အနီးကပ် ရိုက်ပြထားတာမို့
/// စာအုပ်ထဲက စာရွက်နဲ့ တစ်ထပ်တည်း ဖြစ်ပါတယ်. Esc နှိပ်ရင် စာအုပ်ထဲ ရောက်သွားပါတယ်.
[DisallowMultipleComponent]
public class PaperReaderUI : MonoBehaviour
{
    [Header("Paper Model")]
    [Tooltip("Assign Assets/Import/OpenBook/OpenBook.prefab - one of its pages becomes the sheet.")]
    [SerializeField] private GameObject openBookPrefab;
    [SerializeField, Range(0f, 30f)] private float tilt = 14f;
    [Tooltip("Smaller frames the page tighter.")]
    [SerializeField, Range(0.3f, 1.2f)] private float zoom = 0.67f;
    [Tooltip("Which part of the book the camera sits on - default is the centre of its right-hand page.")]
    [SerializeField] private Vector2 pageOffset = new Vector2(0.468f, 0.024f);

    [Header("Layout")]
    [SerializeField, Range(400f, 1200f)] private float panelWidth = 620f;
    [SerializeField, Range(400f, 1200f)] private float panelHeight = 900f;
    [Tooltip("A slight handheld tilt, in degrees either way, so it reads as a found sheet of paper.")]
    [SerializeField, Range(0f, 6f)] private float maxTilt = 2.5f;

    private GameObject _root;
    private CanvasGroup _rootGroup;
    private RectTransform _panel;
    private Text _titleText;
    private Text _bodyText;

    private BookStage _stage;
    private ReadablePaper _current;
    private bool _open;

    private void Awake()
    {
        _stage = new BookStage(openBookPrefab,
            Mathf.RoundToInt(panelWidth), Mathf.RoundToInt(panelHeight), zoom, tilt, pageOffset);

        if (!_stage.IsValid)
        {
            Debug.LogWarning("[PaperReaderUI] No open-book prefab assigned - the note will have no paper.", this);
        }

        BuildUI();
        _root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_stage != null)
        {
            _stage.Dispose();
        }
    }

    private void BuildUI()
    {
        Font font = InventoryUI.DefaultFont();

        Sprite solid = InventoryTextureFactory.CreateSolid(Color.white);
        Sprite vignette = InventoryTextureFactory.CreateVignette(256, 0.25f, 1f, 0.85f);

        // A soft wash so the page's own printed lines fade back behind the story -
        // with a horror flourish of dried blood stains and drips.
        Sprite wash = NoteTextureFactory.CreateTornPaper(
            180, 240, NotePalette.PageWash, NotePalette.PaperAged, 5150493, bloodAmount: 0.8f);

        _root = new GameObject("Paper Reader UI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        _root.transform.SetParent(transform, false);

        Canvas canvas = _root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;

        CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _rootGroup = _root.GetComponent<CanvasGroup>();
        _rootGroup.blocksRaycasts = false;

        RectTransform backdrop = InventoryUI.Stretch(InventoryUI.NewRect("Backdrop", _root.transform), 0f);
        InventoryUI.AddImage(backdrop, solid, InventoryPalette.ScreenDim, Image.Type.Simple, false);

        RectTransform vig = InventoryUI.Stretch(InventoryUI.NewRect("Vignette", _root.transform), 0f);
        InventoryUI.AddImage(vig, vignette, Color.white, Image.Type.Simple, false);

        _panel = InventoryUI.NewRect("Paper", _root.transform);
        _panel.anchorMin = new Vector2(0.5f, 0.5f);
        _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.anchoredPosition = Vector2.zero;
        _panel.sizeDelta = new Vector2(panelWidth, panelHeight);

        RawImage paperImage = _panel.gameObject.AddComponent<RawImage>();
        paperImage.texture = _stage != null ? _stage.Texture : null;
        paperImage.raycastTarget = false;

        // The writable area of the page, inside its margins.
        RectTransform page = InventoryUI.NewRect("Page", _panel);
        page.anchorMin = new Vector2(0.06f, 0.06f);
        page.anchorMax = new Vector2(0.94f, 0.94f);
        page.offsetMin = Vector2.zero;
        page.offsetMax = Vector2.zero;

        RectTransform washRect = InventoryUI.Stretch(InventoryUI.NewRect("Wash", page), -12f);
        InventoryUI.AddImage(washRect, wash, new Color(1f, 1f, 1f, 0.97f), Image.Type.Simple, false);

        RectTransform title = InventoryUI.NewRect("Title", page);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.offsetMin = new Vector2(8f, -50f);
        title.offsetMax = new Vector2(-8f, -10f);
        _titleText = InventoryUI.AddText(title, font, string.Empty, 24,
            TextAnchor.UpperLeft, NotePalette.InkTitle, FontStyle.Bold);

        RectTransform rule = InventoryUI.NewRect("Rule", page);
        rule.anchorMin = new Vector2(0f, 1f);
        rule.anchorMax = new Vector2(1f, 1f);
        rule.pivot = new Vector2(0.5f, 1f);
        rule.offsetMin = new Vector2(8f, -55f);
        rule.offsetMax = new Vector2(-8f, -54f);
        InventoryUI.AddImage(rule, solid, NotePalette.PaperEdge, Image.Type.Simple, false);

        RectTransform body = InventoryUI.NewRect("Body", page);
        body.anchorMin = Vector2.zero;
        body.anchorMax = Vector2.one;
        body.pivot = new Vector2(0.5f, 0.5f);
        body.offsetMin = new Vector2(8f, 12f);
        body.offsetMax = new Vector2(-8f, -66f);
        _bodyText = InventoryUI.AddText(body, font, string.Empty, 19,
            TextAnchor.UpperLeft, NotePalette.InkBody, FontStyle.Normal);
        _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        _bodyText.lineSpacing = 1.15f;

        // The hint sits below the sheet, over the dark backdrop - light text there.
        RectTransform hint = InventoryUI.NewRect("Hint", _panel);
        hint.anchorMin = new Vector2(0f, 0f);
        hint.anchorMax = new Vector2(1f, 0f);
        hint.pivot = new Vector2(0.5f, 0f);
        hint.anchoredPosition = new Vector2(0f, -30f);
        hint.sizeDelta = new Vector2(panelWidth, 26f);
        InventoryUI.AddText(hint, font, "[ ESC ]  PUT IN BOOK", 15,
            TextAnchor.UpperCenter, InventoryPalette.TextFaint, FontStyle.Normal);
    }

    private void Update()
    {
        if (!_open)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Confirm();
        }
    }

    public void Open(ReadablePaper source, string title, string text)
    {
        _current = source;
        _titleText.text = InventoryUI.Track(title);
        _bodyText.text = text;
        _panel.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-maxTilt, maxTilt));

        _root.SetActive(true);
        _rootGroup.alpha = 1f;
        _rootGroup.blocksRaycasts = true;

        _open = true;
        ReaderLock.IsAnyOpen = true;
        PlayerFreeze.Apply(false);

        if (_stage != null)
        {
            _stage.Render();
        }
    }

    private void Confirm()
    {
        ReadablePaper source = _current;
        Close();

        if (source != null)
        {
            source.BeginInsertAnimation();
        }
    }

    private void Close()
    {
        _open = false;
        _current = null;
        _root.SetActive(false);
        _rootGroup.blocksRaycasts = false;
        ReaderLock.IsAnyOpen = false;
        PlayerFreeze.Apply(true);
    }
}
