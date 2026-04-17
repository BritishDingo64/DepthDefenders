using UnityEngine;

public class TeslaZapEffect : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private ParticleSystem particleSystem;

    private void Awake()
    {
        // Create LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.widthMultiplier = 0.1f;
        lineRenderer.useWorldSpace = true;
        
        // Create and assign an emissive cyan material for lightning glow
        Material lineMat = new Material(Shader.Find("Standard"));
        lineMat.color = new Color(0f, 0.96f, 1f, 1f);
        lineMat.SetColor("_EmissionColor", new Color(0f, 0.96f, 1f, 1f) * 2f);
        lineMat.EnableKeyword("_EMISSION");
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
        // Draw jagged lightning line between start and end positions
        if (lineRenderer != null)
        {
            Vector3[] positions = GenerateLightningPath(startPos, endPos);
            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
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

    private Vector3[] GenerateLightningPath(Vector3 start, Vector3 end)
    {
        // Create jagged lightning bolt path
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        int segments = Mathf.Max(4, (int)(distance / 0.5f)); // More segments for longer bolts
        
        Vector3[] positions = new Vector3[segments + 1];
        positions[0] = start;
        positions[segments] = end;

        // Generate randomized intermediate points for lightning effect
        for (int i = 1; i < segments; i++)
        {
            float t = i / (float)segments;
            Vector3 basePos = Vector3.Lerp(start, end, t);
            
            // Random perpendicular offset for jagged effect
            Vector3 perpendicular = Vector3.Cross(direction.normalized, Vector3.up);
            if (perpendicular.sqrMagnitude < 0.001f)
            {
                perpendicular = Vector3.Cross(direction.normalized, Vector3.right);
            }
            perpendicular = perpendicular.normalized;
            
            float offset = Random.Range(-distance * 0.15f, distance * 0.15f);
            positions[i] = basePos + perpendicular * offset;
        }

        return positions;
    }
}
