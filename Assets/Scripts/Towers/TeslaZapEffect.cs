using UnityEngine;

public class TeslaZapEffect : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private ParticleSystem particleSystem;

    private void Awake()
    {
        // Create LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.up);
        lineRenderer.widthMultiplier = 0.15f;
        lineRenderer.useWorldSpace = true;
        
        // Create and assign a cyan material
        Material lineMat = new Material(Shader.Find("Standard"));
        lineMat.color = new Color(0f, 0.96f, 1f, 1f);
        lineMat.SetFloat("_Mode", 3); // Transparent mode
        lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMat.SetInt("_ZWrite", 0);
        lineMat.renderQueue = 3000;
        lineRenderer.material = lineMat;

        // Try to find existing ParticleSystem
        particleSystem = GetComponent<ParticleSystem>();
    }

    public void SetPositions(Vector3 startPos, Vector3 endPos)
    {
        // Draw line between start and end positions
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
        }

        // Position particle system at the target (impact point)
        if (particleSystem != null)
        {
            particleSystem.transform.position = endPos;
        }

        // Orient the effect toward the target
        Vector3 direction = (endPos - startPos).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
