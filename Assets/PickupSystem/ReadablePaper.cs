using System.Collections;
using UnityEngine;

/// <summary>
/// A story page lying in the world. E shows its text in <see cref="PaperReaderUI"/>;
/// pressing Space there sends it flying into the player's book instead of a normal
/// inventory slot - it becomes a page, not a carried item.
/// </summary>
/// အရှေ့မှာ ကျန်နေတဲ့ စာရွက်တစ်ခု. E နှိပ်ရင် PaperReaderUI ထဲမှာ စာသားကို ပြပါတယ်.
/// Space နှိပ်လိုက်ရင် inventory slot ထဲ မထည့်ဘဲ book ရဲ့ page တစ်ခု ဖြစ်သွားပါတယ်.
[DisallowMultipleComponent]
public class ReadablePaper : MonoBehaviour, IInteractable
{
    [Header("Page")]
    [SerializeField] private string pageTitle = "PAGE";
    [Tooltip("What the player reads. Keep it English - uGUI cannot shape Burmese.")]
    [SerializeField, TextArea(6, 20)] private string pageText =
        "Someone was here before us. They didn't leave on their own.";

    [Header("On Pickup")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.7f;

    [Header("Insert Animation")]
    [Tooltip("How long the paper takes to fly into the book once Space is pressed.")]
    [SerializeField, Range(0.1f, 1.5f)] private float insertDuration = 0.4f;
    [Tooltip("Where it flies to, in the camera's local space - roughly toward the HUD corner.")]
    [SerializeField] private Vector3 insertLocalOffset = new Vector3(0.3f, -0.2f, 0.5f);

    public string PageTitle { get { return pageTitle; } }
    public string PageText { get { return pageText; } }

    // --- IInteractable ---------------------------------------------------

    public string Prompt { get { return "READ   " + pageTitle; } }

    public bool CanInteract { get { return true; } }

    /// <summary>Show the text. The page only joins the book once Space confirms it.</summary>
    public string Interact(GameObject interactor)
    {
        PaperReaderUI reader = Object.FindFirstObjectByType<PaperReaderUI>();
        if (reader == null)
        {
            return "NO READER IN SCENE";
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        }

        reader.Open(this, pageTitle, pageText);
        return null;
    }

    /// Called by Unity the first time the component is added, and on "Reset".
    private void Reset()
    {
        pageTitle = name.ToUpperInvariant();

        if (GetComponentInChildren<Collider>() == null)
        {
            FitBoxCollider(gameObject, gameObject.AddComponent<BoxCollider>());
        }
    }

    /// <summary>
    /// Fit a BoxCollider to the imported mesh, whose geometry lives on a child
    /// transform rather than this root - a plain AddComponent&lt;BoxCollider&gt;()
    /// would default to a 1x1x1 box that misses the actual paper entirely.
    /// Shared with the Editor menu, which cannot rely on Reset() always running.
    /// </summary>
    /// Import လုပ်ထားတဲ့ mesh တွေက root ပေါ်မှာ မဟုတ်ဘဲ child transform ပေါ်မှာ
    /// ရှိနေလို့ default BoxCollider က 1x1x1 box ကိုပဲ ဖန်တီးပြီး object ကို လုံးဝ
    /// မဖုံးနိုင်ပါဘူး - child renderer bounds ကို တွက်ပြီး fit လုပ်ပေးရပါတယ်.
    public static void FitBoxCollider(GameObject go, BoxCollider box)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 lossy = go.transform.lossyScale;
        box.center = go.transform.InverseTransformPoint(worldBounds.center);

        // A flat mesh (e.g. a sheet of paper) has zero thickness on one axis -
        // a truly zero-size BoxCollider is degenerate and can miss the aim ray.
        const float minThickness = 0.02f;
        box.size = new Vector3(
            Mathf.Max(lossy.x != 0f ? worldBounds.size.x / lossy.x : worldBounds.size.x, minThickness),
            Mathf.Max(lossy.y != 0f ? worldBounds.size.y / lossy.y : worldBounds.size.y, minThickness),
            Mathf.Max(lossy.z != 0f ? worldBounds.size.z / lossy.z : worldBounds.size.z, minThickness));
    }

    /// <summary>
    /// Called by PaperReaderUI once the player confirms with Space. Plays a short
    /// fly-into-the-book tween - script-driven like Door.cs, no rig and no clip -
    /// then the page joins the book and this object disappears for good.
    /// </summary>
    /// PaperReaderUI ထဲမှာ Space နှိပ်လိုက်ရင် ဒါကို ခေါ်ပါတယ်. Door.cs ပုံစံအတိုင်း
    /// script တင်ပဲ tween လုပ်တာမို့ rig/clip လိုအပ်မှု မရှိပါဘူး.
    public void BeginInsertAnimation()
    {
        StartCoroutine(InsertRoutine());
    }

    private IEnumerator InsertRoutine()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.SetParent(cam.transform, true);
        }

        Vector3 fromPos = transform.localPosition;
        Quaternion fromRot = transform.localRotation;
        Vector3 fromScale = transform.localScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, insertDuration);
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f));

            transform.localPosition = Vector3.Lerp(fromPos, insertLocalOffset, eased);
            transform.localRotation = Quaternion.Slerp(fromRot, Quaternion.identity, eased);
            transform.localScale = Vector3.Lerp(fromScale, Vector3.zero, eased);

            yield return null;
        }

        BookOfPages book = Object.FindFirstObjectByType<BookOfPages>();
        if (book != null)
        {
            book.AddPage(pageTitle, pageText);
        }

        gameObject.SetActive(false);
    }
}
