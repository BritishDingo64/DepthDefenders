using UnityEngine;
using UnityEngine.UI;

// Manages live rotating mini-previews of building prefabs in the build menu UI.
public class BuildingPreviewManager : MonoBehaviour
{
    [Header("Preview Settings")]
    public float previewDistance = 5f;
    public float previewCameraHeight = 0f;
    public float previewLookHeightOffset = 0f;
    public float rotationSpeed = 50f;
    public float previewWorldY = -60f;
    
    [Header("Inspector UI Buttons")]
    public RawImage[] previewImages; // Assign your RawImage components here
    public Button[] previewButtons; // Assign your Button components here
    
    private RenderTexture[] previewTextures;
    private Camera[] previewCameras;
    private GameObject[] previewModels;
    private Transform[] previewAnchors;
    private BuildMenu buildMenu;
    private Transform previewContainer;
    private float lastPreviewDistance;
    private float lastPreviewCameraHeight;
    private float lastPreviewLookHeightOffset;
    private float lastPreviewWorldY;
    
    private const int PREVIEW_WIDTH = 150;
    private const int PREVIEW_HEIGHT = 150;
    
    public void SetupPreviews(GameObject[] buildingPrefabs)
    {
        // Create preview cameras and models for each building prefab.
        buildMenu = GetComponent<BuildMenu>();
        if (buildMenu == null)
        {
            buildMenu = GetComponentInParent<BuildMenu>();
        }

        int configuredBuildingCount = buildingPrefabs == null ? 0 : buildingPrefabs.Length;
        if (configuredBuildingCount <= 0)
        {
            Debug.LogWarning("BuildingPreviewManager: No building prefabs configured.");
            return;
        }

        int imageCount = previewImages == null ? 0 : previewImages.Length;
        int buttonCount = previewButtons == null ? 0 : previewButtons.Length;
        int buildingCount = Mathf.Min(configuredBuildingCount, imageCount, buttonCount);

        if (buildingCount <= 0)
        {
            Debug.LogError("BuildingPreviewManager: Missing preview images or buttons.");
            return;
        }
        
        if (configuredBuildingCount != imageCount || configuredBuildingCount != buttonCount)
        {
            Debug.LogWarning($"BuildingPreviewManager: Count mismatch (prefabs={configuredBuildingCount}, images={imageCount}, buttons={buttonCount}). Using first {buildingCount} entries.");
        }
        
        previewTextures = new RenderTexture[buildingCount];
        previewCameras = new Camera[buildingCount];
        previewModels = new GameObject[buildingCount];
        previewAnchors = new Transform[buildingCount];
        
        // Create a container for preview models
        if (previewContainer != null)
        {
            Destroy(previewContainer.gameObject);
        }

        GameObject previewContainerObject = new GameObject("PreviewModelsContainer");
        previewContainer = previewContainerObject.transform;
        previewContainer.SetParent(null);
        previewContainer.position = new Vector3(0f, previewWorldY, 0f);
        
        // Create preview for each building
        for (int i = 0; i < buildingCount; i++)
        {
            // Create a unique layer for each preview (layers 8-11 for 4 buildings)
            int layerIndex = 8 + i;
            
            // Create render texture
            previewTextures[i] = new RenderTexture(PREVIEW_WIDTH, PREVIEW_HEIGHT, 24);
            
            // Create camera for this preview
            GameObject cameraObj = new GameObject($"PreviewCamera_{i}");
            cameraObj.transform.SetParent(previewContainer);
            previewCameras[i] = cameraObj.AddComponent<Camera>();
            previewCameras[i].targetTexture = previewTextures[i];
            previewCameras[i].clearFlags = CameraClearFlags.SolidColor;
            previewCameras[i].backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            previewCameras[i].fieldOfView = 50f;
            // Only render the layer for this preview
            previewCameras[i].cullingMask = 1 << layerIndex;

            GameObject previewAnchorObject = new GameObject($"PreviewAnchor_{i}");
            previewAnchorObject.transform.SetParent(previewContainer);
            previewAnchorObject.transform.localPosition = Vector3.zero;
            previewAnchorObject.transform.localRotation = Quaternion.identity;
            previewAnchorObject.transform.localScale = Vector3.one;
            previewAnchors[i] = previewAnchorObject.transform;
            
            // Create preview model instance
            previewModels[i] = Instantiate(buildingPrefabs[i], previewAnchors[i]);
            
            // Set the model to the correct layer so only its camera sees it
            SetLayerRecursively(previewModels[i], layerIndex);
            
            // Disable colliders and other gameplay components
            Collider[] colliders = previewModels[i].GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
                col.enabled = false;
            
            // Add spin script
            Spinner spinner = previewAnchorObject.AddComponent<Spinner>();
            spinner.rotationSpeed = rotationSpeed;
            
            // Position model in front of camera
            previewModels[i].transform.localPosition = Vector3.zero;
            CenterPreviewModel(i);
            UpdatePreviewCamera(i);
            
            // Assign render texture to the UI image
            previewImages[i].texture = previewTextures[i];
            
            // Setup button click listener
            int index = i; // Capture for closure
            previewButtons[i].onClick.RemoveAllListeners();
            if (buildMenu != null)
            {
                previewButtons[i].onClick.AddListener(() => buildMenu.SelectBuilding(index));
            }
        }

        CachePreviewSettings();
    }

    private void LateUpdate()
    {
        // Update preview camera positions if preview settings were changed in the inspector.
        if (previewCameras == null || previewModels == null || previewAnchors == null)
            return;

        if (!Mathf.Approximately(lastPreviewDistance, previewDistance) ||
            !Mathf.Approximately(lastPreviewCameraHeight, previewCameraHeight) ||
            !Mathf.Approximately(lastPreviewLookHeightOffset, previewLookHeightOffset) ||
            !Mathf.Approximately(lastPreviewWorldY, previewWorldY))
        {
            UpdatePreviewContainerPosition();
            UpdateAllPreviewCameras();
            CachePreviewSettings();
        }
    }

    private void OnValidate()
    {
        previewDistance = Mathf.Max(0.1f, previewDistance);

        if (previewCameras == null || previewModels == null)
            return;

        UpdatePreviewContainerPosition();
        UpdateAllPreviewCameras();
        CachePreviewSettings();
    }

    private void UpdatePreviewContainerPosition()
    {
        if (previewContainer == null)
            return;

        previewContainer.position = new Vector3(0f, previewWorldY, 0f);
    }

    private void UpdateAllPreviewCameras()
    {
        int previewCount = Mathf.Min(previewCameras.Length, Mathf.Min(previewModels.Length, previewAnchors.Length));
        for (int i = 0; i < previewCount; i++)
        {
            UpdatePreviewCamera(i);
        }
    }

    private void UpdatePreviewCamera(int index)
    {
        // Position the preview camera so the model is framed correctly.
        if (previewCameras == null || previewModels == null || previewAnchors == null)
            return;

        if (index < 0 || index >= previewCameras.Length || index >= previewModels.Length || index >= previewAnchors.Length)
            return;

        Camera previewCamera = previewCameras[index];
        GameObject previewModel = previewModels[index];
        Transform previewAnchor = previewAnchors[index];
        if (previewCamera == null || previewModel == null || previewAnchor == null)
            return;

        Transform cameraTransform = previewCamera.transform;
        Vector3 lookTarget = previewAnchor.position + Vector3.up * previewLookHeightOffset;

        Vector3 cameraBasePosition = previewContainer != null
            ? previewContainer.position
            : previewModel.transform.position;

        cameraTransform.position = cameraBasePosition + new Vector3(0f, previewCameraHeight, -previewDistance);
        cameraTransform.LookAt(lookTarget);
    }

    private void CenterPreviewModel(int index)
    {
        // Center the preview model within its anchor so it looks good in the render texture.
        if (previewModels == null || previewAnchors == null)
            return;

        if (index < 0 || index >= previewModels.Length || index >= previewAnchors.Length)
            return;

        GameObject previewModel = previewModels[index];
        Transform previewAnchor = previewAnchors[index];
        if (previewModel == null || previewAnchor == null)
            return;

        previewModel.transform.localPosition = Vector3.zero;
        previewModel.transform.localRotation = Quaternion.identity;

        Bounds modelBounds = GetModelBounds(previewModel);
        Vector3 localBoundsCenter = previewAnchor.InverseTransformPoint(modelBounds.center);
        previewModel.transform.localPosition -= localBoundsCenter;
    }

    private Bounds GetModelBounds(GameObject previewModel)
    {
        Renderer[] renderers = previewModel.GetComponentsInChildren<Renderer>();
        Renderer primaryRenderer = null;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer)
                continue;

            primaryRenderer = renderers[i];
            break;
        }

        if (primaryRenderer == null)
        {
            if (renderers.Length == 0)
                return new Bounds(previewModel.transform.position, Vector3.one);

            primaryRenderer = renderers[0];
        }

        Bounds combinedBounds = primaryRenderer.bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer)
                continue;

            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        return combinedBounds;
    }

    private void CachePreviewSettings()
    {
        lastPreviewDistance = previewDistance;
        lastPreviewCameraHeight = previewCameraHeight;
        lastPreviewLookHeightOffset = previewLookHeightOffset;
        lastPreviewWorldY = previewWorldY;
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    public void ShowPreviews()
    {
        // No-op preserved for API compatibility. Previews are managed by the scene container and cameras.
    }
    
    public void HidePreviews()
    {
        // No-op preserved for API compatibility. Previews are managed by the scene container and cameras.
    }
}

public class Spinner : MonoBehaviour
{
    public float rotationSpeed = 50f;
    
    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}

// `Spinner` is a tiny utility used by the preview system to rotate preview anchors.
// It is intentionally minimal and only used for cosmetic rotation of preview models.
