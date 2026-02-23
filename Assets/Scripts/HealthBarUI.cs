using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public Health targetHealth;          // Assign your Player's Health component here
    public RectTransform barTransform;   // Assign the green panel RectTransform

    private Vector2 originalSize;

    private void Start()
    {
        if (targetHealth == null)
        {
            Debug.LogWarning("HealthBarUI: No targetHealth assigned!");
            enabled = false;
            return;
        }

        if (barTransform == null)
            barTransform = GetComponent<RectTransform>();

        originalSize = barTransform.sizeDelta;

        targetHealth.onDamaged.AddListener(UpdateBar);
        targetHealth.onHealed.AddListener(UpdateBar);
        targetHealth.onDied.AddListener(UpdateBar);

        UpdateBar();
    }

    private void UpdateBar()
    {
        if (targetHealth == null || barTransform == null) return;

        float pct = Mathf.Clamp01(targetHealth.currentHealth / targetHealth.maxHealth);
        barTransform.sizeDelta = new Vector2(originalSize.x * pct, originalSize.y);
    }
}
