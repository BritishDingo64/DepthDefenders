using TMPro;
using UnityEngine;

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
        if (text != null)
        {
            text.text = value;
        }
    }

    private void Update()
    {
        age += Time.deltaTime;

        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        // Face camera if available so world-space text remains readable.
        if (Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        if (text != null && age >= fadeStartAt)
        {
            float t = Mathf.InverseLerp(fadeStartAt, Mathf.Max(fadeStartAt + 0.01f, lifetime), age);
            Color c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, t);
            text.color = c;
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
