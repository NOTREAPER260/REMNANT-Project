using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders any GameObject into a transparent Sprite, so an inventory slot can
/// show the real object instead of a hand-drawn icon.
/// </summary>
/// Object တစ်ခုကို camera နဲ့ ရိုက်ပြီး Sprite အဖြစ် ပြောင်းပေးတာပါ။
/// ဒါကြောင့် inventory ထဲမှာ ပစ္စည်းရဲ့ တကယ့်ပုံစံအတိုင်း မြင်ရမှာပါ။
///
/// လုပ်ပုံ — မူရင်း object ကို ကူးယူပြီး scene နဲ့ အလှမ်းဝေးတဲ့နေရာ (Y = -9000) မှာ
/// ထားလိုက်တယ်။ ပြီးရင် သီးသန့် camera တစ်လုံးနဲ့ ရိုက်တယ်။ မူရင်း object ကို
/// လုံးဝ မထိပါဘူး။
public static class ObjectPreviewRenderer
{
    /// The rig sits far below the level so no gameplay camera can ever see it.
    private static readonly Vector3 StageOrigin = new Vector3(0f, -9000f, 0f);

    /// <summary>
    /// Photograph <paramref name="source"/> and return the result as a Sprite.
    /// Returns null when the object has nothing to draw.
    /// </summary>
    public static Sprite Render(GameObject source, int size, Vector3 eulerRotation, float padding)
    {
        if (source == null)
        {
            return null;
        }

        size = Mathf.Clamp(size, 32, 512);
        padding = Mathf.Max(padding, 1f);

        int layer = FindFreeLayer();

        // Build the copy inside an inactive parent so its Awake/OnEnable never run.
        GameObject stage = new GameObject("~PreviewStage");
        stage.hideFlags = HideFlags.HideAndDontSave;
        stage.SetActive(false);
        stage.transform.position = StageOrigin;

        GameObject copy = Object.Instantiate(source, stage.transform);
        StripBehaviours(copy);
        SetLayerRecursive(copy, layer);

        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.Euler(eulerRotation);
        copy.transform.localScale = source.transform.lossyScale;

        stage.SetActive(true);
        copy.SetActive(true);

        Bounds bounds;
        if (!TryGetBounds(copy, out bounds))
        {
            Object.DestroyImmediate(stage);
            return null;
        }

        // --- camera ------------------------------------------------------
        GameObject camGo = new GameObject("~PreviewCamera");
        camGo.hideFlags = HideFlags.HideAndDontSave;
        camGo.transform.SetParent(stage.transform, false);

        Camera cam = camGo.AddComponent<Camera>();
        cam.enabled = false;                 // rendered manually, never in the normal loop
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(Mathf.Max(bounds.extents.x, bounds.extents.y) * padding, 0.0001f);
        cam.cullingMask = 1 << layer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.allowHDR = false;
        cam.allowMSAA = true;
        cam.useOcclusionCulling = false;

        float distance = bounds.extents.magnitude + 2f;
        camGo.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - distance);
        camGo.transform.rotation = Quaternion.identity;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = distance * 2f + bounds.size.magnitude + 1f;

        // --- lighting: silence the scene, light the copy ourselves --------
        // Everything from here on runs under try/finally: this method turns the
        // level's lights and fog off for two frames, and leaving them off after
        // an exception would black out the whole game.
        bool fogWas = RenderSettings.fog;
        List<Light> silenced = null;
        RenderTexture rt = null;
        Color32[] onBlack = null;
        Color32[] onWhite = null;

        try
        {
            RenderSettings.fog = false;
            silenced = SilenceSceneLights();

            AddPreviewLight(stage.transform, new Vector3(32f, -35f, 0f), 1.30f,
                new Color(1f, 0.97f, 0.92f));
            AddPreviewLight(stage.transform, new Vector3(-12f, 145f, 0f), 0.55f,
                new Color(0.74f, 0.80f, 0.92f));

            // --- two passes, so alpha is exact regardless of the pipeline -
            rt = RenderTexture.GetTemporary(
                size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            cam.targetTexture = rt;

            onBlack = Capture(cam, rt, Color.black, size);
            onWhite = Capture(cam, rt, Color.white, size);

            cam.targetTexture = null;
        }
        finally
        {
            if (rt != null)
            {
                RenderTexture.ReleaseTemporary(rt);
            }

            RestoreSceneLights(silenced);
            RenderSettings.fog = fogWas;
            Object.DestroyImmediate(stage);
        }

        if (onBlack == null || onWhite == null)
        {
            return null;
        }

        return BuildSprite(onBlack, onWhite, size);
    }

    // ------------------------------------------------------------- capture

    private static Color32[] Capture(Camera cam, RenderTexture rt, Color background, int size)
    {
        cam.backgroundColor = background;
        cam.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readback = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        readback.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
        readback.Apply();
        Color32[] pixels = readback.GetPixels32();

        RenderTexture.active = previous;
        Object.DestroyImmediate(readback);
        return pixels;
    }

    /// <summary>
    /// Recovers colour and coverage from the same frame drawn on black and on white.
    /// Compositing gives black = C*a and white = C*a + (1-a), so a = 1 - (white - black).
    /// </summary>
    /// အနက်ရောင်နောက်ခံနဲ့ အဖြူရောင်နောက်ခံ နှစ်ခါရိုက်ပြီး ကွာခြားချက်ကနေ
    /// alpha ကို တွက်ထုတ်တာပါ။ render pipeline က alpha ကို ဘယ်လိုကိုင်တွယ်တွယ်
    /// ဒီနည်းက အမြဲမှန်ပါတယ်။
    private static Sprite BuildSprite(Color32[] onBlack, Color32[] onWhite, int size)
    {
        bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        Color[] output = new Color[size * size];

        for (int i = 0; i < output.Length; i++)
        {
            Color b = onBlack[i];
            Color w = onWhite[i];
            if (linear)
            {
                b = b.linear;
                w = w.linear;
            }

            float alpha = 1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f;
            alpha = Mathf.Clamp01(alpha);

            if (alpha <= 0.004f)
            {
                output[i] = new Color(0f, 0f, 0f, 0f);
                continue;
            }

            Color c = new Color(b.r / alpha, b.g / alpha, b.b / alpha, 1f);
            if (linear)
            {
                c = c.gamma;
            }

            output[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), alpha);
        }

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixels(output);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    // ------------------------------------------------------------- helpers

    /// <summary>An unnamed layer, so the preview camera can see nothing else.</summary>
    private static int FindFreeLayer()
    {
        for (int i = 31; i >= 8; i--)
        {
            if (string.IsNullOrEmpty(LayerMask.LayerToName(i)))
            {
                return i;
            }
        }
        return 31;
    }

    /// Scripts, colliders and physics have no business running on a photo double.
    private static void StripBehaviours(GameObject root)
    {
        MonoBehaviour[] scripts = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null)
            {
                Object.DestroyImmediate(scripts[i]);
            }
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            Object.DestroyImmediate(bodies[i]);
        }
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            SetLayerRecursive(t.GetChild(i).gameObject, layer);
        }
    }

    private static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        bounds = new Bounds();
        bool found = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderers[i].bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return found;
    }

    private static void AddPreviewLight(Transform parent, Vector3 euler, float intensity, Color color)
    {
        GameObject go = new GameObject("~PreviewLight");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(euler);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
    }

    /// <summary>
    /// Turn every live scene light off for the duration of the capture, so the
    /// icon looks the same no matter where in the level it was picked up.
    /// </summary>
    /// Scene ထဲက မီးအားလုံးကို ခဏပိတ်ထားတာပါ။ ဒါမှ ဘယ်နေရာမှာ ကောက်ကောက်
    /// icon က တစ်ပုံစံတည်း ထွက်မှာပါ။ Render ပြီးတာနဲ့ ချက်ချင်း ပြန်ဖွင့်ပေးပါတယ်။
    private static List<Light> SilenceSceneLights()
    {
        List<Light> silenced = new List<Light>();
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].enabled)
            {
                lights[i].enabled = false;
                silenced.Add(lights[i]);
            }
        }

        return silenced;
    }

    private static void RestoreSceneLights(List<Light> silenced)
    {
        if (silenced == null)
        {
            return;
        }

        for (int i = 0; i < silenced.Count; i++)
        {
            if (silenced[i] != null)
            {
                silenced[i].enabled = true;
            }
        }
    }
}
