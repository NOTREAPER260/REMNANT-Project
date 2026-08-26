using UnityEngine;

/// <summary>
/// Horror-style flashlight feel for a Spot Light: subtle flicker,
/// occasional dying-battery stutter, and a slight handheld lag behind the camera.
/// Put this on the same GameObject as the flashlight's Light component.
/// </summary>
/// အသုံးပြုပုံ — "Spot Light" GameObject ပေါ်မှာ တင်လိုက်ရုံပါပဲ။
/// Light ရဲ့ Intensity ကို Awake မှာ မှတ်ထားပြီး အဲဒီတန်ဖိုးကို အခြေခံပြီး ကစားပါတယ်။
[RequireComponent(typeof(Light))]
public class FlashlightHorrorFeel : MonoBehaviour
{
    [Header("Flicker")]
    [Tooltip("Constant, barely-noticeable brightness wobble.")]
    [SerializeField] private bool enableFlicker = true;
    [SerializeField, Range(0f, 0.5f)] private float flickerAmount = 0.12f;
    [SerializeField, Range(0.5f, 30f)] private float flickerSpeed = 9f;

    [Header("Battery Stutter")]
    [Tooltip("Rare short blackouts, like a bulb about to die.")]
    [SerializeField] private bool enableStutter = true;
    [Tooltip("Average seconds between stutters.")]
    [SerializeField] private float stutterInterval = 14f;
    [SerializeField] private float stutterDuration = 0.18f;

    [Header("Handheld Lag")]
    [Tooltip("The beam trails the camera slightly, as if held in a hand.")]
    [SerializeField] private bool enableSway = true;
    [Tooltip("Higher = the beam catches up with the camera faster.")]
    [SerializeField, Range(1f, 40f)] private float swayResponse = 12f;

    private Light _light;
    private float _baseIntensity;
    private float _noiseSeed;
    private float _nextStutterTime;
    private float _stutterEndTime;
    private Quaternion _swayRotation;

    /// Intensity အခြေခံတန်ဖိုး — script တွေက runtime မှာ ပြောင်းချင်ရင် သုံးလို့ရအောင်။
    public float BaseIntensity
    {
        get { return _baseIntensity; }
        set { _baseIntensity = Mathf.Max(0f, value); }
    }

    private void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
        _noiseSeed = Random.value * 100f;
        _swayRotation = transform.rotation;
        ScheduleNextStutter();
    }

    private void OnEnable()
    {
        _swayRotation = transform.rotation;
        _stutterEndTime = 0f;
        ScheduleNextStutter();
    }

    private void OnDisable()
    {
        // ပိတ်လိုက်ရင် မူလအလင်းအားကို ပြန်ထားပေးမှ Inspector က တန်ဖိုးနဲ့ ကိုက်မယ်။
        if (_light != null)
        {
            _light.intensity = _baseIntensity;
        }
    }

    private void LateUpdate()
    {
        // Camera က LateUpdate မှာ လှည့်တာဖြစ်လို့ sway ကို ဒီမှာပဲ လုပ်ရပါတယ်။
        if (enableSway)
        {
            ApplySway();
        }

        ApplyIntensity();
    }

    private void ApplySway()
    {
        if (transform.parent == null)
        {
            return;
        }

        Quaternion target = transform.parent.rotation;
        float t = 1f - Mathf.Exp(-swayResponse * Time.deltaTime); // framerate-independent damping
        _swayRotation = Quaternion.Slerp(_swayRotation, target, t);
        transform.rotation = _swayRotation;
    }

    private void ApplyIntensity()
    {
        float multiplier = 1f;

        if (enableFlicker)
        {
            // Perlin noise က random ထက် ချောမွေ့လို့ "မီးလှုပ်" သလို ပိုဖြစ်ပါတယ်။
            float n = Mathf.PerlinNoise(_noiseSeed, Time.time * flickerSpeed);
            multiplier *= 1f - flickerAmount * n;
        }

        if (enableStutter)
        {
            if (Time.time >= _nextStutterTime && Time.time > _stutterEndTime)
            {
                _stutterEndTime = Time.time + stutterDuration;
                ScheduleNextStutter();
            }

            if (Time.time < _stutterEndTime)
            {
                bool on = Mathf.PerlinNoise(_noiseSeed + 31.7f, Time.time * 55f) > 0.5f;
                multiplier *= on ? 1f : 0.12f;
            }
        }

        _light.intensity = _baseIntensity * multiplier;
    }

    private void ScheduleNextStutter()
    {
        _nextStutterTime = Time.time + Random.Range(stutterInterval * 0.5f, stutterInterval * 1.5f);
    }
}
