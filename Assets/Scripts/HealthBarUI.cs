using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth targetHealth;
    public Image fillImage;
    public TMP_Text healthText;

    [Header("Display")]
    public bool showHealthNumbers = true;
    public bool useColorGradient = true;
    public Color emptyColor = Color.red;
    public Color fullColor = Color.green;

    private void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        if (targetHealth == null)
            targetHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (targetHealth == null)
        {
            return;
        }

        targetHealth.onDamaged.AddListener(UpdateBar);
        targetHealth.onHealed.AddListener(UpdateBar);
        targetHealth.onDied.AddListener(UpdateBar);
        targetHealth.onHealthChanged.AddListener(UpdateBarFromValue);

        UpdateBar();
    }

    private void OnDisable()
    {
        if (targetHealth == null)
            return;

        targetHealth.onDamaged.RemoveListener(UpdateBar);
        targetHealth.onHealed.RemoveListener(UpdateBar);
        targetHealth.onDied.RemoveListener(UpdateBar);
        targetHealth.onHealthChanged.RemoveListener(UpdateBarFromValue);
    }

    private void UpdateBar()
    {
        if (targetHealth == null) return;

        float pct = Mathf.Clamp01(targetHealth.currentHealth / targetHealth.maxHealth);
        ApplyVisuals(pct);
    }

    private void UpdateBarFromValue(float normalizedValue)
    {
        ApplyVisuals(normalizedValue);
    }

    private void ApplyVisuals(float normalizedValue)
    {
        float pct = Mathf.Clamp01(normalizedValue);

        if (fillImage != null)
        {
            fillImage.fillAmount = pct;

            if (useColorGradient)
            {
                fillImage.color = Color.Lerp(emptyColor, fullColor, pct);
            }
        }

        if (healthText != null)
        {
            healthText.text = showHealthNumbers && targetHealth != null
                ? $"{Mathf.CeilToInt(targetHealth.currentHealth)} / {Mathf.CeilToInt(targetHealth.maxHealth)}"
                : string.Empty;
        }
    }
}
