using UnityEngine;

[ExecuteAlways]
// Applies fog, ambient lighting, and floating particle motes for underwater scenes.
public class UnderwaterAmbientFX : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private bool usePlayerStartAsMotesOrigin = true;
    [SerializeField] private Vector3 motesOriginOverride;
    [SerializeField] private Vector3 motesOriginOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Fog")]
    [SerializeField] private bool applyFog = true;
    [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
    [SerializeField] private Color fogColor = new Color(0.05f, 0.30f, 0.38f, 1f);
    [SerializeField, Range(0.0001f, 0.2f)] private float fogDensity = 0.025f;

    [Header("Ambient")]
    [SerializeField] private bool applyAmbient = true;
    [SerializeField] private Color ambientSkyColor = new Color(0.10f, 0.30f, 0.40f, 1f);
    [SerializeField] private Color ambientEquatorColor = new Color(0.08f, 0.22f, 0.30f, 1f);
    [SerializeField] private Color ambientGroundColor = new Color(0.03f, 0.09f, 0.12f, 1f);
    [SerializeField, Range(0f, 2f)] private float ambientIntensity = 0.9f;

    [Header("Floating Particles")]
    [SerializeField] private bool spawnMotes = true;
    [SerializeField, Range(20, 1200)] private int maxParticles = 350;
    [SerializeField] private Vector3 particleBox = new Vector3(18f, 10f, 18f);
    [SerializeField] private float particleSize = 0.08f;
    [SerializeField] private Vector2 particleSizeRange = new Vector2(0.04f, 0.1f);
    [SerializeField] private Vector2 particleLifetimeRange = new Vector2(8f, 16f);
    [SerializeField] private float particleDrift = 0.25f;
    [SerializeField] private Color particleColor = new Color(0.65f, 0.90f, 1f, 0.28f);
    [SerializeField] private Material particleMaterial;

    private ParticleSystem motes;
    private Material runtimeParticleMaterial;
    private Vector3 motesOrigin;
    private bool hasMotesOrigin;

    private void OnEnable()
    {
        // Called when the script becomes enabled; initialize target, fog, and particles.
        ResolveTarget();
        CaptureMotesOrigin();
        ApplyEnvironment();
        EnsureMotes();
    }

    private void Update()
    {
        ResolveTarget();
        if (!hasMotesOrigin)
            CaptureMotesOrigin();

        ApplyEnvironment();
        UpdateMotes();
    }

    private void OnValidate()
    {
        // Clamp inspector values so the particle system is always valid.
        particleBox.x = Mathf.Max(1f, particleBox.x);
        particleBox.y = Mathf.Max(1f, particleBox.y);
        particleBox.z = Mathf.Max(1f, particleBox.z);
        particleSize = Mathf.Max(0.01f, particleSize);
        particleSizeRange.x = Mathf.Max(0.005f, particleSizeRange.x);
        particleSizeRange.y = Mathf.Max(particleSizeRange.x, particleSizeRange.y);
        particleLifetimeRange.x = Mathf.Max(0.5f, particleLifetimeRange.x);
        particleLifetimeRange.y = Mathf.Max(particleLifetimeRange.x, particleLifetimeRange.y);
        particleDrift = Mathf.Max(0f, particleDrift);
        if (enabled)
        {
            ApplyEnvironment();
            EnsureMotes();
        }
    }

    private void ResolveTarget()
    {
        // Find the player transform in the scene to anchor the underwater effect.
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
    }

    private void CaptureMotesOrigin()
    {
        // Capture the starting origin for the particle motes, either from the player or a fixed override.
        if (usePlayerStartAsMotesOrigin && playerTarget != null)
        {
            motesOrigin = playerTarget.position + motesOriginOffset;
            hasMotesOrigin = true;
            return;
        }

        motesOrigin = motesOriginOverride + motesOriginOffset;
        hasMotesOrigin = true;
    }

    private void ApplyEnvironment()
    {
        // Apply fog and ambient lighting settings for the underwater look.
        if (applyFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
        }

        if (applyAmbient)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
            RenderSettings.ambientIntensity = ambientIntensity;
        }
    }

    private void EnsureMotes()
    {
        // Create or configure the underwater mote particle system.
        if (!spawnMotes)
        {
            if (motes != null)
                motes.gameObject.SetActive(false);
            return;
        }

        if (motes == null)
        {
            Transform existing = transform.Find("UnderwaterMotes");
            if (existing != null)
            {
                motes = existing.GetComponent<ParticleSystem>();
            }
        }

        if (motes == null)
        {
            GameObject go = new GameObject("UnderwaterMotes");
            go.transform.SetParent(transform, false);
            motes = go.AddComponent<ParticleSystem>();
        }

        var main = motes.main;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetimeRange.x, particleLifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleDrift * 0.3f, particleDrift);
        main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Min(particleSize, particleSizeRange.x), Mathf.Max(particleSize, particleSizeRange.y));
        main.startColor = particleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.gravityModifier = 0f;

        var emission = motes.emission;
        emission.enabled = true;
        emission.rateOverTime = Mathf.Max(3f, maxParticles * 0.18f);

        var shape = motes.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = particleBox;
        shape.randomDirectionAmount = 0.2f;

        var velocity = motes.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-particleDrift * 0.15f, particleDrift * 0.15f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
        velocity.z = new ParticleSystem.MinMaxCurve(-particleDrift * 0.15f, particleDrift * 0.15f);

        var noise = motes.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.08f;
        noise.damping = true;

        var colorOverLifetime = motes.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient alphaGradient = new Gradient();
        alphaGradient.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(1f, 1f, 1f, 1f), 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.15f),
                new GradientAlphaKey(0.9f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(alphaGradient);

        var sizeOverLifetime = motes.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f),
            new Keyframe(0.4f, 1f),
            new Keyframe(1f, 0.85f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var renderer = motes.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.minParticleSize = 0.0001f;
        renderer.maxParticleSize = 0.03f;
        EnsureParticleMaterial(renderer);

        motes.Clear(true);

        if (!motes.isPlaying)
            motes.Play();

        motes.gameObject.SetActive(true);
    }

    private void UpdateMotes()
    {
        // Keep the mote emitter positioned relative to the origin anchor.
        if (motes == null || !spawnMotes)
            return;

        if (!hasMotesOrigin)
            CaptureMotesOrigin();

        motes.transform.position = motesOrigin;
    }

    private void EnsureParticleMaterial(ParticleSystemRenderer renderer)
    {
        // Make sure the particle renderer has a usable material for the underwater motes.
        if (renderer == null) return;

        if (particleMaterial != null)
        {
            renderer.sharedMaterial = particleMaterial;
            return;
        }

        if (runtimeParticleMaterial == null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            if (shader != null)
            {
                runtimeParticleMaterial = new Material(shader);
                runtimeParticleMaterial.name = "RuntimeUnderwaterMotesMaterial";

                if (runtimeParticleMaterial.HasProperty("_BaseColor"))
                    runtimeParticleMaterial.SetColor("_BaseColor", particleColor);

                if (runtimeParticleMaterial.HasProperty("_Color"))
                    runtimeParticleMaterial.SetColor("_Color", particleColor);

                if (runtimeParticleMaterial.HasProperty("_Surface"))
                    runtimeParticleMaterial.SetFloat("_Surface", 1f);

                if (runtimeParticleMaterial.HasProperty("_Blend"))
                    runtimeParticleMaterial.SetFloat("_Blend", 0f);
            }
        }

        if (runtimeParticleMaterial != null)
            renderer.sharedMaterial = runtimeParticleMaterial;
    }
}
