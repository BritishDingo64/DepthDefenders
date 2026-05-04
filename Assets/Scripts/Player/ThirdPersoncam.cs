using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Controls the third-person camera behavior and player facing direction for basic and combat modes.
public class ThirdPersoncam : MonoBehaviour {
    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerobj;
    public Rigidbody rb;
    public Transform combatlookat;
    public float rotationSpeed;
    public CameraStyle currentstyle;
    public GameObject CombatCam;
    public GameObject FreeLookCam;
    [Header("Debug")]
    public bool logOnStart = true;
    private BuildMenu buildMenu;
    public enum CameraStyle {
        basic,
        Combat
    }
    // Camera control modes: `basic` = free-look player-facing controls, `Combat` = lock to combat target direction.
    public void Start() {
        // Lock cursor for gameplay and initialize camera mode.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        switchcameraStyle(CameraStyle.basic);
        buildMenu = FindFirstObjectByType<BuildMenu>();

        if (logOnStart) {
            Vector3 startViewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
            bool startVectorIsZero = startViewDir.sqrMagnitude <= 0.0001f;
            Debug.Log($"ThirdPersoncam initialized. Zero-vector guard active. Start view vector zero: {startVectorIsZero}");
        }
    }
    private void Update() {
        // Don't lock/control camera if build menu is open
        if (buildMenu != null && buildMenu.IsMenuOpen())
            return;
        
        //rotation orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        if (viewDir.sqrMagnitude > 0.0001f) {
            orientation.forward = viewDir.normalized;
        }

        // Rotate player based on the current camera style.
        if (currentstyle == CameraStyle.basic) {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

            if (inputDirection != Vector3.zero) {
                Vector3 combinedDirection = (orientation.forward * verticalInput + orientation.right * horizontalInput).normalized;
                playerobj.forward = Vector3.Slerp(playerobj.forward, combinedDirection, Time.deltaTime * rotationSpeed);
            }
        } else if (currentstyle == CameraStyle.Combat) {
            Vector3 dirtocombatlookat = combatlookat.position - new Vector3(transform.position.x, combatlookat.position.y, transform.position.z);
            if (dirtocombatlookat.sqrMagnitude > 0.0001f) {
                Vector3 combatDir = dirtocombatlookat.normalized;
                orientation.forward = combatDir;
                playerobj.forward = combatDir;
            }
        }
    }
    private void switchcameraStyle(CameraStyle newStyle) {
        // Enable only the selected camera style.
        CombatCam.SetActive(false);
        FreeLookCam.SetActive(false);
        CombatCam.SetActive(newStyle == CameraStyle.Combat);
        FreeLookCam.SetActive(newStyle == CameraStyle.basic);

        currentstyle = newStyle;
    }
}
