using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Full-screen page viewer opened from the inventory's BOOK slot. The book is
/// the real OpenBook model, rendered live by <see cref="BookStage"/>; the story
/// text is laid over its right-hand page while the model's own illustration
/// carries the left. Shows the newest page first - A steps to an older page,
/// D steps back toward the newest.
/// </summary>
/// Inventory ထဲက BOOK slot ကနေ ဖွင့်တဲ့ page viewer ပါ. စာအုပ်က တကယ့် OpenBook
/// 3D model ကို render လုပ်ပြထားတာဖြစ်ပြီး, ကောက်ထားတဲ့ စာရွက်ရဲ့ စာသားကို ညာဘက်
/// စာမျက်နှာပေါ်မှာ တင်ပြပါတယ်. A/D နဲ့ စာမျက်နှာ လှန်နိုင်ပါတယ်.
[DisallowMultipleComponent]
public class BookReaderUI : MonoBehaviour
{
    [Header("Book Model")]
    [Tooltip("Assign Assets/Import/OpenBook/OpenBook.prefab.")]
    [SerializeField] private GameObject openBookPrefab;
    [Tooltip("How far the book leans back from the camera.")]
    [SerializeField, Range(0f, 30f)] private float tilt = 14f;
    [Tooltip("Smaller frames the book tighter.")]
    [SerializeField, Range(0.5f, 1.4f)] private float zoom = 0.80f;

    [Header("Layout")]
    [SerializeField, Range(600f, 1400f)] private float panelWidth = 1100f;
    [SerializeField, Range(300f, 900f)] private float panelHeight = 780f;

    [Header("Page Turn")]
    [SerializeField, Range(0.05f, 0.8f)] private float turnDuration = 0.22f;
    [SerializeField, Range(0f, 25f)] private float turnSwing = 9f;

    private GameObject _root;
    private CanvasGroup _rootGroup;
    private Text _counterText;
    private Text _titleText;
    private Text _bodyText;

    private BookStage _stage;
    private IList<BookOfPages.PageEntry> _pages;
    private int _index;
    private bool _open;
    private Coroutine _turn;

    private void Awake()
    {
        _stage = new BookStage(openBookPrefab,
            Mathf.RoundToInt(panelWidth), Mathf.RoundToInt(panelHeight), zoom, tilt, Vector2.zero);

        if (!_stage.IsValid)
        {
            Debug.LogWarning("[BookReaderUI] No open-book prefab assigned - the reader will have no book.", this);
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

        // A soft-edged wash so the model's own printed lines fade back and the
        // story text on top stays readable - with a horror flourish of dried
        // blood. Left and right get their own seed so the stains don't mirror.
        Sprite washLeft = NoteTextureFactory.CreateTornPaper(
            180, 300, NotePalette.PageWash, NotePalette.PaperAged, 20260905, bloodAmount: 0.8f);
        Sprite washRight = NoteTextureFactory.CreateTornPaper(
            180, 300, NotePalette.PageWash, NotePalette.PaperAged, 20260906, bloodAmount: 0.8f);

        _root = new GameObject("Book Reader UI",
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

        RectTransform panel = InventoryUI.NewRect("Book", _root.transform);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(panelWidth, panelHeight);

        RawImage bookImage = panel.gameObject.AddComponent<RawImage>();
        bookImage.texture = _stage != null ? _stage.Texture : null;
        bookImage.raycastTarget = false;

        // Page rectangles measured against the rendered frame: the model's two
        // pages sit at these fractions of the image, left and right of the spine.
        RectTransform leftPage = PageRect("LeftPage", panel, 0.145f, 0.495f);
        RectTransform rightPage = PageRect("RightPage", panel, 0.505f, 0.905f);

        // Both pages get a near-opaque wash first, blanking out the model's own
        // baked-in handwriting and illustration so only our own text shows.
        RectTransform leftWash = InventoryUI.Stretch(InventoryUI.NewRect("Wash", leftPage), -14f);
        InventoryUI.AddImage(leftWash, washLeft, new Color(1f, 1f, 1f, 0.97f), Image.Type.Simple, false);

        RectTransform counter = InventoryUI.Stretch(InventoryUI.NewRect("Counter", leftPage), 12f);
        _counterText = InventoryUI.AddText(counter, font, string.Empty, 17,
            TextAnchor.LowerCenter, NotePalette.InkFaint, FontStyle.Normal);

        RectTransform washRect = InventoryUI.Stretch(InventoryUI.NewRect("Wash", rightPage), -14f);
        InventoryUI.AddImage(washRect, washRight, new Color(1f, 1f, 1f, 0.97f), Image.Type.Simple, false);

        RectTransform title = InventoryUI.NewRect("Title", rightPage);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.offsetMin = new Vector2(6f, -46f);
        title.offsetMax = new Vector2(-6f, -8f);
        _titleText = InventoryUI.AddText(title, font, string.Empty, 22,
            TextAnchor.UpperLeft, NotePalette.InkTitle, FontStyle.Bold);

        RectTransform rule = InventoryUI.NewRect("Rule", rightPage);
        rule.anchorMin = new Vector2(0f, 1f);
        rule.anchorMax = new Vector2(1f, 1f);
        rule.pivot = new Vector2(0.5f, 1f);
        rule.offsetMin = new Vector2(6f, -51f);
        rule.offsetMax = new Vector2(-6f, -50f);
        InventoryUI.AddImage(rule, solid, NotePalette.PaperEdge, Image.Type.Simple, false);

        RectTransform body = InventoryUI.NewRect("Body", rightPage);
        body.anchorMin = Vector2.zero;
        body.anchorMax = Vector2.one;
        body.pivot = new Vector2(0.5f, 0.5f);
        body.offsetMin = new Vector2(6f, 10f);
        body.offsetMax = new Vector2(-6f, -62f);
        _bodyText = InventoryUI.AddText(body, font, string.Empty, 17,
            TextAnchor.UpperLeft, NotePalette.InkBody, FontStyle.Normal);
        _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        _bodyText.lineSpacing = 1.15f;

        // The hint sits below the book, over the dark backdrop - light text there.
        RectTransform hint = InventoryUI.NewRect("Hint", panel);
        hint.anchorMin = new Vector2(0f, 0f);
        hint.anchorMax = new Vector2(1f, 0f);
        hint.pivot = new Vector2(0.5f, 0f);
        hint.anchoredPosition = new Vector2(0f, -30f);
        hint.sizeDelta = new Vector2(panelWidth, 26f);
        InventoryUI.AddText(hint, font,
            "[ A ]  PREV          [ D ]  NEXT          [ ESC ]  CLOSE", 15,
            TextAnchor.UpperCenter, InventoryPalette.TextFaint, FontStyle.Normal);
    }

    /// One page of the rendered spread, as a fraction of the image's width.
    private static RectTransform PageRect(string name, RectTransform parent, float uMin, float uMax)
    {
        RectTransform rect = InventoryUI.NewRect(name, parent);
        rect.anchorMin = new Vector2(uMin, 0.06f);
        rect.anchorMax = new Vector2(uMax, 0.96f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void Update()
    {
        if (!_open)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (_pages == null || _pages.Count == 0)
        {
            return;
        }

        if (keyboard.aKey.wasPressedThisFrame)
        {
            SetIndex(Mathf.Min(_index + 1, _pages.Count - 1), -turnSwing);
        }
        else if (keyboard.dKey.wasPressedThisFrame)
        {
            SetIndex(Mathf.Max(_index - 1, 0), turnSwing);
        }
    }

    public void Open(IList<BookOfPages.PageEntry> pages)
    {
        _pages = pages;
        _index = 0;

        _root.SetActive(true);
        _rootGroup.alpha = 1f;
        _rootGroup.blocksRaycasts = true;

        _open = true;
        ReaderLock.IsAnyOpen = true;
        PlayerFreeze.Apply(false);

        Refresh();

        if (_stage != null)
        {
            _stage.SetYaw(0f);
            _stage.Render();
        }
    }

    private void SetIndex(int index, float swing)
    {
        if (index == _index)
        {
            return;
        }

        _index = index;
        Refresh();

        if (_turn != null)
        {
            StopCoroutine(_turn);
        }
        _turn = StartCoroutine(TurnPage(swing));
    }

    /// <summary>
    /// The book swings back to square over a few frames. The stage camera only
    /// renders while this runs, so the render texture costs nothing at rest.
    /// </summary>
    private IEnumerator TurnPage(float swing)
    {
        if (_stage == null || !_stage.IsValid)
        {
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, turnDuration);
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f));

            _stage.SetYaw(Mathf.Lerp(swing, 0f, eased));
            _stage.Render();
            yield return null;
        }

        _stage.SetYaw(0f);
        _stage.Render();
        _turn = null;
    }

    private void Refresh()
    {
        if (_pages == null || _pages.Count == 0)
        {
            _counterText.text = string.Empty;
            _titleText.text = InventoryUI.Track("EMPTY");
            _bodyText.text = "No pages collected yet.";
            return;
        }

        BookOfPages.PageEntry page = _pages[_index];
        _counterText.text = (_index + 1) + " / " + _pages.Count;
        _titleText.text = InventoryUI.Track(page.Title);
        _bodyText.text = page.Text;
    }

    private void Close()
    {
        _open = false;
        _root.SetActive(false);
        _rootGroup.blocksRaycasts = false;
        ReaderLock.IsAnyOpen = false;
        PlayerFreeze.Apply(true);
    }
}
