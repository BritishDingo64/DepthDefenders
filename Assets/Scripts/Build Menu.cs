using UnityEngine;
using TMPro;

public class BuildMenu : MonoBehaviour
{
    [Header("UI References")]
    public Canvas buildMenuCanvas;
    
    [Header("Building Prefabs")]
    public GameObject[] buildingPrefabs; // Array of 4 building prefabs
    
    [Header("Preview Settings")]
    public Material validPlacementMaterial; // Green material
    public Material invalidPlacementMaterial; // Red material
    public LayerMask groundLayer; // What counts as valid ground
    public BuildingPreviewManager previewManager; // Assign this in Inspector
    
    private bool isMenuOpen = false;
    private bool isPlacingBuilding = false;
    private GameObject currentPreview;
    private int selectedBuildingIndex = -1;
    private Renderer[] previewRenderers;
    
    void Start()
    {
        // Setup preview manager
        if (previewManager != null)
            previewManager.SetupPreviews(buildingPrefabs);
    }

    void Update()
    {
        // Toggle build menu with B key
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (isPlacingBuilding)
            {
                // Cancel building placement
                CancelPlacement();
            }
            else
            {
                // Toggle menu
                ToggleMenu();
            }
        }
        
        // Select building with number keys 1-4
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectBuilding(0);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectBuilding(1);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SelectBuilding(2);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            SelectBuilding(3);
        
        // Handle building placement
        if (isPlacingBuilding)
        {
            UpdatePreviewPosition();
            
            // Place building with left mouse button
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceBuilding();
            }
        }
    }
    
    void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (buildMenuCanvas != null)
            buildMenuCanvas.gameObject.SetActive(isMenuOpen);
        
        // Unlock mouse when menu is open, lock when closed
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
    
    public bool IsMenuOpen()
    {
        return isMenuOpen;
    }
    
    // Call this from UI buttons (0-3)
    public void SelectBuilding(int buildingIndex)
    {
        if (buildingIndex < 0 || buildingIndex >= buildingPrefabs.Length)
            return;
        
        selectedBuildingIndex = buildingIndex;
        StartPlacement();
    }
    
    void StartPlacement()
    {
        isPlacingBuilding = true;
        isMenuOpen = false;
        
        // Hide menu
        if (buildMenuCanvas != null)
            buildMenuCanvas.gameObject.SetActive(false);
        
        // Create preview object
        if (buildingPrefabs[selectedBuildingIndex] != null)
        {
            currentPreview = Instantiate(buildingPrefabs[selectedBuildingIndex]);
            
            // Get all renderers for material swapping
            previewRenderers = currentPreview.GetComponentsInChildren<Renderer>();
            
            // Disable colliders on preview
            Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            
            // Make preview semi-transparent
            SetPreviewTransparency();
        }
    }
    
    void UpdatePreviewPosition()
    {
        if (currentPreview == null)
            return;
        
        // Raycast from camera to mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            currentPreview.transform.position = hit.point;
            
            // Check if placement is valid
            bool isValid = IsValidPlacement(hit.point);
            
            // Update preview color
            Material materialToUse = isValid ? validPlacementMaterial : invalidPlacementMaterial;
            foreach (Renderer rend in previewRenderers)
            {
                rend.material = materialToUse;
            }
        }
    }
    
    bool IsValidPlacement(Vector3 position)
    {
        // Check if the position is on valid ground
        // You can add more complex checks here (overlapping with other buildings, etc.)
        Collider[] overlaps = Physics.OverlapSphere(position, 1f);
        
        foreach (Collider col in overlaps)
        {
            // Check if overlapping with other buildings
            if (col.CompareTag("Building"))
                return false;
        }
        
        return true;
    }
    
    void TryPlaceBuilding()
    {
        if (currentPreview == null)
            return;
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            if (IsValidPlacement(hit.point))
            {
                // Place the actual building
                GameObject building = Instantiate(buildingPrefabs[selectedBuildingIndex], hit.point, currentPreview.transform.rotation);
                
                // Clean up and return to menu
                CancelPlacement();
                ToggleMenu(); // Reopen menu after placing
            }
        }
    }
    
    void CancelPlacement()
    {
        isPlacingBuilding = false;
        
        // Destroy preview
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }
    
    void SetPreviewTransparency()
    {
        // Make the preview materials semi-transparent
        foreach (Renderer rend in previewRenderers)
        {
            foreach (Material mat in rend.materials)
            {
                // Set rendering mode to transparent
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                
                // Set alpha to make it faded
                Color color = mat.color;
                color.a = 0.5f;
                mat.color = color;
            }
        }
    }
}
