using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Adds a global post-processing Volume for the washed-out, desaturated horror look
/// (Neutral tonemapping, vignette, film grain, slight desaturation). The scene had no
/// Volume at all, so the flashlight's bright hotspot was hard-clipping to a flat white
/// disc at close range instead of rolling off smoothly.
/// Run via Tools > Horror Game > Setup Atmosphere Volume.
/// </summary>
public static class HorrorAtmosphereSetup
{
    private const string ProfilePath = "Assets/Settings/HorrorAtmosphere.asset";

    [MenuItem("Tools/Horror Game/Setup Atmosphere Volume")]
    public static void SetupAtmosphere()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Atmosphere setup: exit Play Mode first, then run this again.");
            return;
        }

        GameObject volumeGO = GameObject.Find("Horror Atmosphere Volume");
        if (volumeGO == null)
        {
            volumeGO = new GameObject("Horror Atmosphere Volume");
            Undo.RegisterCreatedObjectUndo(volumeGO, "Create Horror Atmosphere Volume");
        }

        Volume volume = volumeGO.GetComponent<Volume>();
        if (volume == null) volume = Undo.AddComponent<Volume>(volumeGO);
        volume.isGlobal = true;
        volume.weight = 1f;

        VolumeProfile profile = volume.sharedProfile;
        if (profile == null)
        {
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            volume.sharedProfile = profile;
        }

        if (!profile.TryGet(out Tonemapping tonemapping))
        {
            tonemapping = profile.Add<Tonemapping>(true);
        }
        tonemapping.active = true;
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.Neutral;

        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>(true);
        }
        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.35f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.6f;

        if (!profile.TryGet(out FilmGrain filmGrain))
        {
            filmGrain = profile.Add<FilmGrain>(true);
        }
        filmGrain.active = true;
        filmGrain.type.overrideState = true;
        filmGrain.type.value = FilmGrainLookup.Thin1;
        filmGrain.intensity.overrideState = true;
        filmGrain.intensity.value = 0.25f;
        filmGrain.response.overrideState = true;
        filmGrain.response.value = 0.8f;

        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }
        colorAdjustments.active = true;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = -25f;
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = new Color(0.92f, 1f, 0.95f);

        EditorUtility.SetDirty(profile);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        FixCookieImportSettings();

        Debug.Log("Horror Atmosphere Volume ready. Press Play to see the flashlight's hotspot roll off instead of clipping to white.");
    }

    /// <summary>
    /// The flashlight's cookie was imported as a plain sRGB texture, which gamma-decodes the
    /// grayscale mask before it reaches the light and flattens the contrast between the bright
    /// core and the dimmer ring - part of why the "small dot" hotspot wasn't reading clearly.
    /// Re-importing it as a proper Cookie texture (linear, mask taken straight from grayscale)
    /// fixes that without touching the artwork itself.
    /// </summary>
    private static void FixCookieImportSettings()
    {
        const string cookiePath = "Assets/Art/Lighting/T_FlashlightCookie.png";
        TextureImporter importer = AssetImporter.GetAtPath(cookiePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("Atmosphere setup: could not find " + cookiePath + " to fix its import settings.");
            return;
        }

        importer.textureType = TextureImporterType.Cookie;
        importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        importer.sRGBTexture = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }
}
