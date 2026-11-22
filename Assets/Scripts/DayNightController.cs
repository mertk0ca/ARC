using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[ExecuteAlways]
public class DayNightController : MonoBehaviour
{
    [Header("Güneş")]
    [SerializeField] Transform sun;
    [SerializeField, Range(0f, 24f)] float hourOfDay = 12f;
    [SerializeField] float sunAzimuth = 188f;
    [SerializeField] float maxSunElevation = 58f;

    [Header("İç ışıklar")]
    [SerializeField] GameObject livingRoomLight;
    [SerializeField] GameObject livingRoomAreaLight;
    [SerializeField] GameObject livingRoomCeilingAreaLight;
    [SerializeField] Transform firstFloorLed;
    [SerializeField] float nightStartHour = 18f;
    [SerializeField] float nightEndHour = 6f;
    [SerializeField] float ledEmissiveIntensity = 6f;
    [SerializeField, Range(0f, 1f)] float dayInteriorFill = 0.16f;
    [SerializeField] float dayExposureCompensation = 2.2f;

    static readonly Color LedEmissiveLdr = new Color(0.75f, 0.74f, 0.68f, 1f);

    readonly List<Renderer> ledWhites = new List<Renderer>();
    readonly List<Material> ledMaterials = new List<Material>();
    MaterialPropertyBlock ledBlock;
    SemicircleClockSlider clock;
    bool ledsCached;
    Exposure exposureSettings;
    float originalExposureCompensation;
    bool exposureCached;

    public float HourOfDay
    {
        get => hourOfDay;
        set => ApplyHour(value);
    }

    void OnEnable()
    {
        if (sun == null)
            sun = transform;

        CacheLedWhites();
        if (Application.isPlaying)
        {
            EnsureEventSystem();
            if (clock == null)
            {
                clock = FindFirstObjectByType<SemicircleClockSlider>();
                if (clock == null)
                    clock = SemicircleClockSlider.Create();
                clock.HourChanged -= ApplyHour;
                clock.HourChanged += ApplyHour;
            }
        }

        ApplyHour(hourOfDay);
    }

    void OnDisable()
    {
        if (clock != null)
            clock.HourChanged -= ApplyHour;
        RestoreExposure();
    }

    void OnValidate()
    {
        ledsCached = false;
        if (!isActiveAndEnabled)
            return;
        ApplyHour(hourOfDay);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    void CacheLedWhites()
    {
        ledWhites.Clear();
        ledMaterials.Clear();
        ledsCached = true;
        if (firstFloorLed == null)
            return;

        Renderer[] renderers = firstFloorLed.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].name != "LedWhite")
                continue;

            ledWhites.Add(renderers[i]);
            if (!Application.isPlaying)
                continue;

            Material[] materials = renderers[i].materials;
            for (int m = 0; m < materials.Length; m++)
                ledMaterials.Add(materials[m]);
        }

        ledBlock ??= new MaterialPropertyBlock();
    }

    public void ApplyHour(float hour)
    {
        hourOfDay = Mathf.Clamp(hour, 0f, 24f);
        if (hourOfDay >= 24f)
            hourOfDay = 0f;

        bool isNight = hourOfDay >= nightStartHour || hourOfDay < nightEndHour;
        UpdateSun(hourOfDay);

        if (livingRoomLight != null)
            livingRoomLight.SetActive(true);

        float interiorDimmer = isNight ? 1f : dayInteriorFill;
        SetInteriorLight(livingRoomAreaLight, interiorDimmer);
        SetInteriorLight(livingRoomCeilingAreaLight, interiorDimmer);

        if (!ledsCached)
            CacheLedWhites();
        SetLedEmission(isNight);
        ApplyExposure(isNight);

        if (clock != null)
            clock.SetHour(hourOfDay, isNight);
    }

    void UpdateSun(float hour)
    {
        if (sun == null)
            return;

        float elevation;
        if (hour > 6f && hour < 18f)
        {
            float dayT = Mathf.InverseLerp(6f, 18f, hour);
            elevation = Mathf.Sin(dayT * Mathf.PI) * maxSunElevation;
        }
        else
            elevation = -15f;

        sun.rotation = Quaternion.Euler(elevation, sunAzimuth, 0f);

        Light sunLight = sun.GetComponent<Light>();
        if (sunLight != null)
            sunLight.enabled = true;

        HDAdditionalLightData hdSun = sun.GetComponent<HDAdditionalLightData>();
        if (hdSun != null)
        {
            hdSun.lightDimmer = 1f;
            if (hour > 6f && hour < 18f)
                hdSun.intensity = 130000f;
            else
                hdSun.intensity = 800f;
        }

        if (sunLight != null)
            sunLight.bounceIntensity = hour > 6f && hour < 18f ? 2.5f : 1f;
    }

    static void SetInteriorLight(GameObject lightObject, float dimmer)
    {
        if (lightObject == null)
            return;

        bool on = dimmer > 0.02f;
        lightObject.SetActive(on);
        if (!on)
            return;

        HDAdditionalLightData hd = lightObject.GetComponent<HDAdditionalLightData>();
        if (hd != null)
        {
            hd.lightDimmer = dimmer;
            hd.volumetricDimmer = dimmer * 0.35f;
        }
    }

    void ApplyExposure(bool isNight)
    {
        if (!Application.isPlaying)
            return;

        CacheExposure();
        if (exposureSettings == null)
            return;

        exposureSettings.compensation.overrideState = true;
        exposureSettings.compensation.value = isNight ? 0f : dayExposureCompensation;
    }

    void CacheExposure()
    {
        if (exposureCached)
            return;

        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++)
        {
            VolumeProfile profile = volumes[i].sharedProfile;
            if (profile == null || !profile.TryGet(out Exposure exposure))
                continue;

            exposureSettings = exposure;
            originalExposureCompensation = exposure.compensation.value;
            exposureCached = true;
            return;
        }
    }

    void RestoreExposure()
    {
        if (!exposureCached || exposureSettings == null)
            return;
        exposureSettings.compensation.value = originalExposureCompensation;
    }

    void SetLedEmission(bool on)
    {
        float intensity = on ? ledEmissiveIntensity : 0f;
        Color ldr = on ? LedEmissiveLdr : Color.black;
        Color hdr = ldr * intensity;

        if (Application.isPlaying && ledMaterials.Count > 0)
        {
            for (int i = 0; i < ledMaterials.Count; i++)
                ApplyLedMaterial(ledMaterials[i], hdr, ldr, intensity, on);
            return;
        }

        ledBlock ??= new MaterialPropertyBlock();
        for (int i = 0; i < ledWhites.Count; i++)
        {
            Renderer renderer = ledWhites[i];
            if (renderer == null)
                continue;
            renderer.GetPropertyBlock(ledBlock);
            ledBlock.SetColor("_EmissiveColor", hdr);
            ledBlock.SetColor("_EmissiveColorLDR", ldr);
            ledBlock.SetFloat("_EmissiveIntensity", intensity);
            ledBlock.SetFloat("_UseEmissiveIntensity", on ? 1f : 0f);
            renderer.SetPropertyBlock(ledBlock);
        }
    }

    static void ApplyLedMaterial(Material mat, Color hdr, Color ldr, float intensity, bool on)
    {
        if (mat == null)
            return;
        if (mat.HasProperty("_EmissiveColor"))
            mat.SetColor("_EmissiveColor", hdr);
        if (mat.HasProperty("_EmissiveColorLDR"))
            mat.SetColor("_EmissiveColorLDR", ldr);
        if (mat.HasProperty("_EmissiveIntensity"))
            mat.SetFloat("_EmissiveIntensity", intensity);
        if (mat.HasProperty("_UseEmissiveIntensity"))
            mat.SetFloat("_UseEmissiveIntensity", on ? 1f : 0f);
    }
}
