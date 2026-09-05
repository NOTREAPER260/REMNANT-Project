using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps every page the player has fed into the book, newest first, and grants
/// the book itself as a permanent inventory item at the start of the game -
/// same pattern as the flashlight in HorrorInventory.
/// </summary>
/// ကောက်ယူခဲ့တဲ့ page အားလုံးကို (အသစ်ဆုံးအရင်) သိမ်းထားပြီး, ဂိမ်းစချိန်မှာ
/// စာအုပ်ကို ချလို့မရတဲ့ inventory item အဖြစ် အလိုအလျောက် ထည့်ပေးပါတယ် - ဓာတ်မီး
/// ရဲ့ pattern အတိုင်းပါ.
///
/// [DefaultExecutionOrder] guarantees this runs its Start() after
/// HorrorInventory's, so the book cannot land in a slot the flashlight
/// then overwrites (Unity does not otherwise promise sibling Start() order).
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class BookOfPages : MonoBehaviour
{
    public class PageEntry
    {
        public readonly string Title;
        public readonly string Text;

        public PageEntry(string title, string text)
        {
            Title = title;
            Text = text;
        }
    }

    [Header("Wiring")]
    [Tooltip("Leave empty to find the inventory in the scene.")]
    [SerializeField] private HorrorInventory inventory;
    [Tooltip("Leave empty to find it in the scene.")]
    [SerializeField] private BookReaderUI readerUI;

    [Header("Book Item")]
    [SerializeField] private string bookDisplayName = "BOOK";
    [SerializeField, TextArea(2, 4)]
    private string bookDescription = "Pages collected along the way end up here.";
    [Tooltip("Photographed for the inventory icon. Assign Assets/Import/Book/Book.prefab.")]
    [SerializeField] private GameObject bookPrefab;
    [SerializeField, Range(64, 512)] private int iconSize = 192;
    [SerializeField] private Vector3 iconRotation = new Vector3(18f, -28f, 0f);
    [SerializeField, Range(1f, 2f)] private float iconPadding = 1.18f;

    private readonly List<PageEntry> _pages = new List<PageEntry>();

    private void Start()
    {
        if (inventory == null)
        {
            inventory = Object.FindFirstObjectByType<HorrorInventory>();
        }
        if (readerUI == null)
        {
            readerUI = Object.FindFirstObjectByType<BookReaderUI>();
        }

        GrantBook();
    }

    private void GrantBook()
    {
        if (inventory == null)
        {
            Debug.LogWarning("[BookOfPages] No HorrorInventory in scene - the book was not added.", this);
            return;
        }

        Sprite icon = bookPrefab != null
            ? ObjectPreviewRenderer.Render(bookPrefab, iconSize, iconRotation, iconPadding)
            : null;

        InventoryItem item = new InventoryItem(bookDisplayName, bookDescription, icon, false);

        // No world object to put back, same as the flashlight - so it can never be dropped.
        item.WorldObject = null;
        item.Droppable = false;
        item.OnUse = OpenReader;

        if (!inventory.TryAddItem(item))
        {
            Debug.LogWarning("[BookOfPages] No room for the book in the inventory.", this);
        }
    }

    /// <summary>Called by a ReadablePaper once its insert animation finishes.</summary>
    /// ReadablePaper ရဲ့ insert animation ပြီးတဲ့အခါ ဒါကို ခေါ်ပါတယ်.
    public void AddPage(string title, string text)
    {
        _pages.Insert(0, new PageEntry(title, text));
    }

    /// <summary>Wired to the book InventoryItem's OnUse - opens the page viewer.</summary>
    public void OpenReader()
    {
        if (readerUI == null)
        {
            readerUI = Object.FindFirstObjectByType<BookReaderUI>();
        }

        if (inventory != null)
        {
            inventory.Close();
        }

        if (readerUI != null)
        {
            readerUI.Open(_pages);
        }
    }
}
