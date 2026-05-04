using TMPro;
using UnityEngine;

// Displays floating damage text above targets and fades it out over time.
public class DamagePopup : MonoBehaviour
{
    [Header("References")]
    public TMP_Text text;

    [Header("Animation")]
    public float lifetime = 0.8f;
    public float floatSpeed = 1.2f;
    public float fadeStartAt = 0.4f;

    private float age;
    private Color baseColor = Color.white;

    private void Awake()
    {
        // Cache the TextMeshPro component if not already assigned.
        if (text == null)
        {
            text = GetComponentInChildren<TMP_Text>();
        }

        if (text != null)
        {
            baseColor = text.color;
        }
    }

    public void Initialize(string value)
    {
        // Initialize the popup text. `value` is the string shown (for example a damage number).
        if (text != null)
        {
            text.text = value;
        }
    }

    private void Update()
    {
        age += Time.deltaTime;

        // Float the popup upward.
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        // Rotate the popup to face the camera so it stays readable.
        if (Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        // Fade out after fadeStartAt seconds.
        if (text != null && age >= fadeStartAt)
        {
            float t = Mathf.InverseLerp(fadeStartAt, Mathf.Max(fadeStartAt + 0.01f, lifetime), age);
            Color c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, t);
            text.color = c;
        }

        // Destroy the popup once its lifetime has ended.
        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
