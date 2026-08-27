using UnityEngine;

/// <summary>
/// A door that swings open and shut when the player interacts with it.
/// The swing is driven by script, not by an Animator, so it works on any door
/// mesh with no clips, no rig and no setup.
/// </summary>
/// တံခါးကို E နှိပ်ပြီး ဖွင့်/ပိတ်လို့ရအောင် လုပ်ပေးတာပါ။
/// Animation clip မလိုပါဘူး — code နဲ့ တဖြည်းဖြည်း လှည့်ပေးတာမို့
/// ဘယ် တံခါး mesh မဆို setup မလိုဘဲ အလုပ်လုပ်ပါတယ်။
///
/// **အရေးကြီးတာ —** တံခါးရဲ့ pivot က **အထစ် (hinge) အနားမှာ** ရှိရပါမယ်။
/// အလယ်မှာ ရှိနေရင် တံခါးက အလယ်ပတ်လည် လှည့်သွားပါလိမ့်မယ်။
/// အဲဒီအခါ တံခါးကို empty GameObject တစ်ခုနဲ့ ထုပ်ပြီး အဲဒီ empty ကို
/// အထစ်နေရာမှာ ထားလိုက်ပါ၊ ပြီးရင် Door ကို အဲဒီ empty ပေါ်မှာ တင်ပါ။
[DisallowMultipleComponent]
public class Door : MonoBehaviour, IInteractable
{
    [Header("Label")]
    [Tooltip("Shown in the prompt. English only - uGUI cannot shape Burmese.")]
    [SerializeField] private string displayName = "DOOR";

    [Header("Swing")]
    [SerializeField, Range(20f, 170f)] private float openAngle = 95f;
    [SerializeField, Range(0.1f, 3f)] private float openDuration = 0.7f;
    [Tooltip("The hinge, in the door's own local space. Y is upright for almost every door.")]
    [SerializeField] private Vector3 hingeAxis = Vector3.up;
    [Tooltip("Which way the door faces. Leave at zero to work it out from the mesh.")]
    [SerializeField] private Vector3 facingAxis = Vector3.zero;
    [Tooltip("Swing away from whoever opened it, so it never sweeps through the player.")]
    [SerializeField] private bool openAwayFromPlayer = true;
    [SerializeField] private bool startsOpen;

    [Header("Locked")]
    [SerializeField] private bool locked;
    [SerializeField] private string lockedMessage = "LOCKED";

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    private Quaternion _closedRotation;
    private float _fromAngle;
    private float _toAngle;
    private float _currentAngle;
    private float _progress = 1f;
    private bool _isOpen;

    public bool IsOpen { get { return _isOpen; } }

    public bool Locked
    {
        get { return locked; }
        set { locked = value; }
    }

    public string Prompt
    {
        get { return (_isOpen ? "CLOSE   " : "OPEN   ") + displayName; }
    }

    public bool CanInteract { get { return true; } }

    private void Reset()
    {
        displayName = "DOOR";

        // The aim ray needs something to hit.
        if (GetComponentInChildren<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    private void Awake()
    {
        _closedRotation = transform.localRotation;

        if (facingAxis == Vector3.zero)
        {
            facingAxis = DetectFacingAxis();
        }

        if (startsOpen)
        {
            _isOpen = true;
            _currentAngle = openAngle;
            _fromAngle = openAngle;
            _toAngle = openAngle;
            ApplyRotation();
        }
    }

    private void Update()
    {
        if (_progress >= 1f)
        {
            return;
        }

        _progress = Mathf.Min(1f, _progress + Time.deltaTime / Mathf.Max(0.01f, openDuration));

        // Mathf.SmoothStep interpolates BETWEEN _fromAngle and _toAngle with easing -
        // that is exactly what is wanted here. (It is NOT GLSL smoothstep; see
        // InventoryTextureFactory.Step01 for the other meaning.)
        _currentAngle = Mathf.SmoothStep(_fromAngle, _toAngle, _progress);
        ApplyRotation();
    }

    public string Interact(GameObject interactor)
    {
        if (locked)
        {
            Play(lockedSound);
            return lockedMessage;
        }

        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open(interactor);
        }

        return null;
    }

    public void Open(GameObject interactor)
    {
        float sign = 1f;

        if (openAwayFromPlayer && interactor != null)
        {
            // Positive means the player stands on the door's facing side, so the
            // door has to swing the other way to get out of their way.
            Vector3 normal = transform.TransformDirection(facingAxis);
            sign = Vector3.Dot(interactor.transform.position - transform.position, normal) > 0f ? -1f : 1f;
        }

        StartSwing(sign * openAngle);
        _isOpen = true;
        Play(openSound);
    }

    public void Close()
    {
        StartSwing(0f);
        _isOpen = false;
        Play(closeSound);
    }

    private void StartSwing(float target)
    {
        _fromAngle = _currentAngle;
        _toAngle = target;
        _progress = 0f;
    }

    private void ApplyRotation()
    {
        Vector3 axis = hingeAxis.sqrMagnitude < 1e-6f ? Vector3.up : hingeAxis.normalized;
        transform.localRotation = _closedRotation * Quaternion.AngleAxis(_currentAngle, axis);
    }

    /// <summary>
    /// A door leaf is a flat slab: its thinnest horizontal axis is the way it faces.
    /// </summary>
    /// တံခါးက ပြားပြားဖြစ်လို့ အပါးဆုံးဝင်ရိုးက တံခါးမျက်နှာမူရာ ဖြစ်ပါတယ်။
    private Vector3 DetectFacingAxis()
    {
        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            Vector3 size = filter.sharedMesh.bounds.size;
            return size.x <= size.z ? Vector3.right : Vector3.forward;
        }

        return Vector3.forward;
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }
    }
}
