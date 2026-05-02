using System.Collections.Generic;
using UnityEngine;

// Generates animated underwater light rays around the player and orients them toward the camera.
public class UnderwaterLightRays : MonoBehaviour
{
    [System.Serializable]
    private class RayInstance
    {
        public Transform transform;
        public Renderer renderer;
        public MaterialPropertyBlock propertyBlock;
        public float angle;
        public float radius;
        public float width;
        public float height;
        public float yOffset;
        public float pulseOffset;
        public float nextRelocateTime;
        public bool isNear;
    }

    [Header("Tracking")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Camera viewerCamera;
    [SerializeField] private bool usePlayerStartAsOrigin = true;
    [SerializeField] private Vector3 worldOriginOverride;

    [Header("Ray Material")]
    [SerializeField] private Material rayMaterial;
    [SerializeField] private Color nearRayColor = new Color(0.68f, 0.93f, 1f, 0.5f);
    [SerializeField] private Color farRayColor = new Color(0.62f, 0.86f, 1f, 0.3f);

    [Header("Counts")]
    [SerializeField, Min(0)] private int nearRayCount = 14;
    [SerializeField, Min(0)] private int farRayCount = 22;

    [Header("Placement")]
    [SerializeField] private float mapCoverageRadius = 90f;
    [SerializeField] private float nearRadius = 16f;
    [SerializeField] private float farRadiusMin = 30f;
    [SerializeField] private float farRadiusMax = 75f;
    [SerializeField] private float minRayHeight = 14f;
    [SerializeField] private float maxRayHeight = 28f;
    [SerializeField] private float minRayWidth = 1.8f;
    [SerializeField] private float maxRayWidth = 4.2f;
    [SerializeField] private float rayCenterYOffset = 5f;

    [Header("Animation")]
    [SerializeField] private float driftSpeed = 0.18f;
    [SerializeField] private float swayAmount = 0.9f;
    [SerializeField] private float relocateEveryMin = 6f;
    [SerializeField] private float relocateEveryMax = 14f;

    private readonly List<RayInstance> rays = new List<RayInstance>();
    private Vector3 worldOrigin;
    private bool hasWorldOrigin;

    private void Start()
    {
        // Initialize references, determine the ray origin, and create ray quads.
        ResolveReferences();
        CaptureWorldOrigin();
        EnsureMaterial();
        BuildRays();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (viewerCamera == null)
        {
            return;
        }

        if (!hasWorldOrigin)
        {
            CaptureWorldOrigin();
            if (!hasWorldOrigin)
                return;
        }

        float time = Time.time;
        Vector3 anchor = worldOrigin;

        for (int i = 0; i < rays.Count; i++)
        {
            RayInstance ray = rays[i];
            if (ray == null || ray.transform == null)
            {
                continue;
            }

            if (time >= ray.nextRelocateTime)
            {
                RelocateRay(ray, time);
            }

            ray.angle += driftSpeed * Time.deltaTime * (ray.isNear ? 1f : 0.65f);
            float r = Mathf.Max(0.1f, ray.radius + Mathf.Sin(time * 0.35f + ray.pulseOffset) * (ray.isNear ? 0.8f : 1.8f));
            r = Mathf.Min(r, mapCoverageRadius);

            Vector3 planarOffset = new Vector3(Mathf.Cos(ray.angle), 0f, Mathf.Sin(ray.angle)) * r;
            Vector3 pos = anchor + planarOffset;
            pos.y = anchor.y + rayCenterYOffset + ray.yOffset + Mathf.Sin(time * 0.9f + ray.pulseOffset) * swayAmount;
            ray.transform.position = pos;

            // Billboard to viewer
            ray.transform.rotation = Quaternion.LookRotation(viewerCamera.transform.forward, Vector3.up);
            ray.transform.localScale = new Vector3(ray.width, ray.height, 1f);

            if (ray.renderer != null)
            {
                Color c = ray.isNear ? nearRayColor : farRayColor;
                float pulse = 0.85f + 0.15f * Mathf.Sin(time * 0.7f + ray.pulseOffset);
                c.a *= pulse;

                ray.propertyBlock.SetColor("_Color", c);
                ray.renderer.SetPropertyBlock(ray.propertyBlock);
            }
        }
    }

    private void OnDisable()
    {
        // Clean up generated ray objects when the script is disabled.
        ClearRays();
    }

    private void OnValidate()
    {
        mapCoverageRadius = Mathf.Max(5f, mapCoverageRadius);
        nearRadius = Mathf.Max(0f, nearRadius);
        nearRadius = Mathf.Min(nearRadius, mapCoverageRadius * 0.7f);
        farRadiusMin = Mathf.Max(nearRadius + 1f, farRadiusMin);
        farRadiusMax = Mathf.Max(farRadiusMin + 1f, farRadiusMax);
        farRadiusMin = Mathf.Min(farRadiusMin, mapCoverageRadius - 1f);
        farRadiusMax = Mathf.Min(farRadiusMax, mapCoverageRadius);
        minRayHeight = Mathf.Max(1f, minRayHeight);
        maxRayHeight = Mathf.Max(minRayHeight, maxRayHeight);
        minRayWidth = Mathf.Max(0.1f, minRayWidth);
        maxRayWidth = Mathf.Max(minRayWidth, maxRayWidth);
    }

    private void CaptureWorldOrigin()
    {
        // Determine the world space origin used for ray placement.
        if (usePlayerStartAsOrigin)
        {
            if (playerTarget == null)
                ResolveReferences();

            if (playerTarget != null)
            {
                worldOrigin = playerTarget.position;
                hasWorldOrigin = true;
                return;
            }
        }

        worldOrigin = worldOriginOverride;
        hasWorldOrigin = true;
    }

    private void ResolveReferences()
    {
        // Find the player and camera references if they are not already assigned.
        if (playerTarget == null)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
                playerTarget = taggedPlayer.transform;
        }

        if (playerTarget == null)
        {
            Movement movement = FindFirstObjectByType<Movement>();
            if (movement != null)
                playerTarget = movement.transform;
        }

        if (viewerCamera == null)
            viewerCamera = Camera.main;
    }

    private void EnsureMaterial()
    {
        if (rayMaterial != null)
            return;

        Shader s = Shader.Find("Custom/UnderwaterLightRay");
        if (s != null)
        {
            rayMaterial = new Material(s);
            rayMaterial.name = "RuntimeUnderwaterLightRayMaterial";
        }
    }

    private void BuildRays()
    {
        // Create the specified number of near and far ray quads.
        ClearRays();

        int total = Mathf.Max(0, nearRayCount) + Mathf.Max(0, farRayCount);
        for (int i = 0; i < total; i++)
        {
            bool isNear = i < nearRayCount;
            RayInstance ray = CreateRay(isNear, Time.time);
            if (ray != null)
            {
                rays.Add(ray);
            }
        }
    }

    private RayInstance CreateRay(bool isNear, float time)
    {
        // Create a new quad to represent a single light ray.
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = isNear ? "NearLightRay" : "FarLightRay";
        go.transform.SetParent(transform, false);

        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            if (rayMaterial != null)
                rend.sharedMaterial = rayMaterial;

            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        RayInstance ray = new RayInstance
        {
            transform = go.transform,
            renderer = rend,
            propertyBlock = new MaterialPropertyBlock(),
            angle = Random.Range(0f, Mathf.PI * 2f),
            radius = isNear ? Random.Range(0.5f, nearRadius) : Random.Range(farRadiusMin, farRadiusMax),
            width = Random.Range(minRayWidth, maxRayWidth),
            height = Random.Range(minRayHeight, maxRayHeight),
            yOffset = Random.Range(-1.5f, 1.5f),
            pulseOffset = Random.Range(0f, 100f),
            isNear = isNear
        };

        RelocateRay(ray, time);
        return ray;
    }

    private void RelocateRay(RayInstance ray, float now)
    {
        // Pick a new random angle and radius so the ray slowly moves around the origin.
        if (ray == null) return;

        ray.angle = Random.Range(0f, Mathf.PI * 2f);
        ray.radius = ray.isNear
            ? Random.Range(0.5f, nearRadius)
            : Random.Range(farRadiusMin, farRadiusMax);
        ray.radius = Mathf.Min(ray.radius, mapCoverageRadius);

        ray.nextRelocateTime = now + Random.Range(relocateEveryMin, relocateEveryMax);
    }

    private void ClearRays()
    {
        // Destroy or remove all generated ray objects.
        for (int i = 0; i < rays.Count; i++)
        {
            if (rays[i] != null && rays[i].transform != null)
            {
                if (Application.isPlaying)
                    Destroy(rays[i].transform.gameObject);
                else
                    DestroyImmediate(rays[i].transform.gameObject);
            }
        }

        rays.Clear();
    }
}
