using UnityEngine;

public class SceneLightingController : MonoBehaviour
{
    public bool startAsDay = true;
    public Light sunLight;

    [Header("Day")]
    public Color daySkyColor = new Color(0.82f, 0.86f, 0.92f, 1f);
    public Color dayEquatorColor = new Color(0.68f, 0.7f, 0.72f, 1f);
    public Color dayGroundColor = new Color(0.52f, 0.52f, 0.5f, 1f);
    public float dayAmbientIntensity = 2.4f;
    public float daySunIntensity = 3.2f;
    public float dayShadowStrength = 0.65f;
    public float dayReflectionIntensity = 1.35f;
    public Vector3 daySunEulerAngles = new Vector3(62f, -35f, 0f);
    public float daySceneLightMultiplier = 0.25f;

    [Header("Night")]
    public Color nightSkyColor = new Color(0.13f, 0.16f, 0.22f, 1f);
    public Color nightEquatorColor = new Color(0.08f, 0.09f, 0.12f, 1f);
    public Color nightGroundColor = new Color(0.04f, 0.04f, 0.05f, 1f);
    public float nightAmbientIntensity = 0.65f;
    public float nightSunIntensity = 0.35f;
    public float nightShadowStrength = 0.9f;
    public float nightReflectionIntensity = 0.75f;
    public Vector3 nightSunEulerAngles = new Vector3(18f, -120f, 0f);
    public float nightSceneLightMultiplier = 1.8f;

    private Material daySkybox;
    private Material nightSkybox;
    private Light[] sceneLights;
    private float[] sceneLightBaseIntensities;

    public bool IsDay { get; private set; }
    public string CurrentLabel => IsDay ? "白天" : "黑夜";

    private void Awake()
    {
        if (sunLight == null)
        {
            sunLight = RenderSettings.sun != null ? RenderSettings.sun : FindDirectionalLight();
        }

        CacheSceneLights();
        daySkybox = RenderSettings.skybox;
        nightSkybox = CreateNightSkybox();
        ApplyLighting(startAsDay);
    }

    public void ToggleDayNight()
    {
        ApplyLighting(!IsDay);
    }

    public void ApplyLighting(bool day)
    {
        IsDay = day;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = day ? daySkyColor : nightSkyColor;
        RenderSettings.ambientEquatorColor = day ? dayEquatorColor : nightEquatorColor;
        RenderSettings.ambientGroundColor = day ? dayGroundColor : nightGroundColor;
        RenderSettings.ambientIntensity = day ? dayAmbientIntensity : nightAmbientIntensity;
        RenderSettings.reflectionIntensity = day ? dayReflectionIntensity : nightReflectionIntensity;
        RenderSettings.skybox = day ? daySkybox : nightSkybox;

        if (sunLight != null)
        {
            sunLight.gameObject.SetActive(true);
            sunLight.enabled = true;
            sunLight.intensity = day ? daySunIntensity : nightSunIntensity;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = day ? dayShadowStrength : nightShadowStrength;
            sunLight.transform.rotation = Quaternion.Euler(day ? daySunEulerAngles : nightSunEulerAngles);
            RenderSettings.sun = sunLight;
        }

        ApplySceneLightMultiplier(day ? daySceneLightMultiplier : nightSceneLightMultiplier);
        DynamicGI.UpdateEnvironment();
    }

    private void CacheSceneLights()
    {
        sceneLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        sceneLightBaseIntensities = new float[sceneLights.Length];

        for (int i = 0; i < sceneLights.Length; i++)
        {
            sceneLightBaseIntensities[i] = sceneLights[i] != null ? sceneLights[i].intensity : 0f;
        }
    }

    private void ApplySceneLightMultiplier(float multiplier)
    {
        if (sceneLights == null || sceneLightBaseIntensities == null)
        {
            return;
        }

        for (int i = 0; i < sceneLights.Length; i++)
        {
            Light sceneLight = sceneLights[i];
            if (sceneLight == null || sceneLight == sunLight)
            {
                continue;
            }

            sceneLight.intensity = sceneLightBaseIntensities[i] * multiplier;
        }
    }

    private static Light FindDirectionalLight()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
            {
                return lights[i];
            }
        }

        return null;
    }

    private static Material CreateNightSkybox()
    {
        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader)
        {
            name = "Runtime Night Skybox"
        };

        material.SetColor("_SkyTint", new Color(0.16f, 0.18f, 0.28f, 1f));
        material.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.03f, 1f));
        material.SetFloat("_Exposure", 0.55f);
        material.SetFloat("_AtmosphereThickness", 0.65f);
        return material;
    }
}
