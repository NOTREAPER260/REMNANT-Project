using UnityEngine;

/// <summary>
/// Keeps a copy of the open-book model on a hidden stage far below the level
/// and renders it into a RenderTexture on demand, so the book reader can show
/// the real 3D model instead of a drawn panel. Same trick as
/// ObjectPreviewRenderer, but the stage stays alive between renders.
/// </summary>
/// စာအုပ် model ကို ကစားကွင်းအောက် အလွန်ဝေးတဲ့နေရာမှာ ထားပြီး camera တစ်လုံးနဲ့
/// RenderTexture ထဲ ရိုက်ယူတာပါ. ဒါကြောင့် UI ထဲမှာ တကယ့် 3D စာအုပ်ကို မြင်ရမှာပါ.
/// ObjectPreviewRenderer နဲ့ နည်းလမ်းတူပေမယ့် ဒီ stage က ဖျက်မပစ်ဘဲ ဆက်ရှိနေပါတယ်.
public class BookStage
{
    /// Far from the icon renderer's own stage at (0, -9000, 0), so neither
    /// camera can ever catch the other's subject in frame.
    private static readonly Vector3 StageOrigin = new Vector3(5000f, -9000f, 0f);

    /// Every stage stands well clear of the last one - two stages sharing a spot
    /// would land in each other's shot, since they also share a culling layer.
    private static int _stageCount;

    private GameObject _stage;
    private Transform _book;
    private Camera _camera;
    private RenderTexture _texture;
    private float _tilt;

    public RenderTexture Texture { get { return _texture; } }

    public bool IsValid { get { return _stage != null && _camera != null && _book != null; } }

    /// <param name="cameraOffset">Shifts the framing, to sit on one page rather than the spread.</param>
    public BookStage(GameObject prefab, int width, int height, float orthographicSize, float tiltDegrees,
                     Vector2 cameraOffset)
    {
        if (prefab == null)
        {
            return;
        }

        _tilt = tiltDegrees;
        int layer = FindFreeLayer();

        _stage = new GameObject("~BookStage");
        _stage.hideFlags = HideFlags.HideAndDontSave;
        _stage.transform.position = StageOrigin + new Vector3(_stageCount * 200f, 0f, 0f);
        _stageCount++;

        GameObject book = Object.Instantiate(prefab, _stage.transform);
        book.transform.localPosition = Vector3.zero;
        book.transform.localRotation = Quaternion.Euler(_tilt, 0f, 0f);
        StripBehaviours(book);
        SetLayerRecursive(book, layer);
        _book = book.transform;

        GameObject camGo = new GameObject("~BookCamera");
        camGo.hideFlags = HideFlags.HideAndDontSave;
        camGo.transform.SetParent(_stage.transform, false);
        camGo.transform.localPosition = new Vector3(cameraOffset.x, cameraOffset.y, -5f);
        camGo.transform.localRotation = Quaternion.identity;

        _camera = camGo.AddComponent<Camera>();
        _camera.enabled = false;                  // rendered by hand, never in the normal loop
        _camera.orthographic = true;
        _camera.orthographicSize = orthographicSize;
        _camera.cullingMask = 1 << layer;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.nearClipPlane = 0.01f;
        _camera.farClipPlane = 20f;
        _camera.allowHDR = false;
        _camera.useOcclusionCulling = false;

        // The stage lights only touch this layer, so the level's own lighting
        // never changes how the book looks.
        AddLight(new Vector3(35f, -20f, 0f), 1.6f, layer, new Color(1f, 0.97f, 0.92f));
        AddLight(new Vector3(-15f, 150f, 0f), 0.7f, layer, new Color(0.78f, 0.82f, 0.92f));

        _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        _texture.antiAliasing = 4;
        _texture.hideFlags = HideFlags.HideAndDontSave;
        _texture.Create();
        _camera.targetTexture = _texture;
    }

    /// <summary>Swing the book about its spine - the page-turn flourish.</summary>
    public void SetYaw(float degrees)
    {
        if (_book != null)
        {
            _book.localRotation = Quaternion.Euler(_tilt, degrees, 0f);
        }
    }

    public void Render()
    {
        if (_camera != null)
        {
            _camera.Render();
        }
    }

    public void Dispose()
    {
        if (_camera != null)
        {
            _camera.targetTexture = null;
        }

        if (_texture != null)
        {
            _texture.Release();
            Object.Destroy(_texture);
            _texture = null;
        }

        if (_stage != null)
        {
            Object.Destroy(_stage);
            _stage = null;
        }
    }

    /// <summary>An unnamed layer, so the stage camera can see nothing else.</summary>
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

    private void AddLight(Vector3 euler, float intensity, int layer, Color color)
    {
        GameObject go = new GameObject("~BookLight");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(_stage.transform, false);
        go.transform.localRotation = Quaternion.Euler(euler);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = color;
        light.shadows = LightShadows.None;
        light.cullingMask = 1 << layer;
    }

    private static void StripBehaviours(GameObject root)
    {
        MonoBehaviour[] scripts = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null)
            {
                Object.Destroy(scripts[i]);
            }
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.Destroy(colliders[i]);
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
}
