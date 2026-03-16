using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class UnderwaterSunSource : MonoBehaviour
{
    [Header("Base Underwater Sun")]
    [SerializeField] private Color underwaterColor = new Color(0.62f, 0.84f, 0.93f, 1f);
    [SerializeField] private float baseIntensity = 0.7f;
    [SerializeField] private float shadowStrength = 0.45f;
    [SerializeField] private float shadowBias = 0.08f;
    [SerializeField] private float shadowNormalBias = 0.45f;

    [Header("Direction")]
    [SerializeField] private Vector3 baseEuler = new Vector3(65f, -25f, 0f);
    [SerializeField] private bool animateDirection = true;
    [SerializeField] private float directionSway = 1.2f;
    [SerializeField] private float directionSwaySpeed = 0.25f;

    [Header("Light Shimmer")]
    [SerializeField] private bool animateShimmer = true;
    [SerializeField] private float shimmerAmount = 0.18f;
    [SerializeField] private float shimmerSpeed = 0.7f;

    private Light cachedLight;

    private void Awake()
    {
        cachedLight = GetComponent<Light>();
        ApplyBaseSettings();
    }

    private void OnEnable()
    {
        if (cachedLight == null) cachedLight = GetComponent<Light>();
        ApplyBaseSettings();
    }

    private void OnValidate()
    {
        if (cachedLight == null) cachedLight = GetComponent<Light>();
        ApplyBaseSettings();
    }

    private void Update()
    {
        if (cachedLight == null) return;

        float t = Application.isPlaying ? Time.time : (float)System.DateTime.Now.TimeOfDay.TotalSeconds;

        if (animateDirection)
        {
            float swayX = Mathf.Sin(t * directionSwaySpeed) * directionSway;
            float swayY = Mathf.Cos(t * (directionSwaySpeed * 0.73f)) * directionSway;
            transform.rotation = Quaternion.Euler(baseEuler.x + swayX, baseEuler.y + swayY, baseEuler.z);
        }
        else
        {
            transform.rotation = Quaternion.Euler(baseEuler);
        }

        if (animateShimmer)
        {
            float shimmer = Mathf.Sin(t * shimmerSpeed) * 0.5f + Mathf.Sin(t * shimmerSpeed * 1.93f) * 0.5f;
            float intensityMul = 1f + shimmer * shimmerAmount;
            cachedLight.intensity = Mathf.Max(0f, baseIntensity * intensityMul);
        }
        else
        {
            cachedLight.intensity = Mathf.Max(0f, baseIntensity);
        }
    }

    private void ApplyBaseSettings()
    {
        if (cachedLight == null) return;

        cachedLight.type = LightType.Directional;
        cachedLight.color = underwaterColor;
        cachedLight.intensity = Mathf.Max(0f, baseIntensity);
        cachedLight.shadows = LightShadows.Soft;
        cachedLight.shadowStrength = Mathf.Clamp01(shadowStrength);
        cachedLight.shadowBias = Mathf.Max(0f, shadowBias);
        cachedLight.shadowNormalBias = Mathf.Max(0f, shadowNormalBias);
    }
}
