using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThirdPersoncam : MonoBehaviour
{
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
        public enum CameraStyle
    {
        basic,
        Combat
    }
    public void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        switchcameraStyle(CameraStyle.basic);
    }
    private void Update()
    {
        //rotation orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;

        //rotate player object
        if (currentstyle == CameraStyle.basic)
        {
        float horizontalInput = Input.GetAxis("Horizontal");
             float verticalInput = Input.GetAxis("Vertical");    
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if(inputDirection != Vector3.zero)
        {
            Vector3 combinedDirection = (orientation.forward * verticalInput + orientation.right * horizontalInput).normalized;
            playerobj.forward = Vector3.Slerp(playerobj.forward, combinedDirection, Time.deltaTime * rotationSpeed);
        }}
        else if (currentstyle == CameraStyle.Combat)
        {
            Vector3 dirtocombatlookat = combatlookat.position - new Vector3(transform.position.x, combatlookat.position.y, transform.position.z);
            orientation.forward = dirtocombatlookat.normalized;
          
          playerobj.forward = dirtocombatlookat.normalized;
        }
    }
    private void switchcameraStyle(CameraStyle newStyle)
    {
        CombatCam.SetActive(false);
        FreeLookCam.SetActive(false);
        CombatCam.SetActive(newStyle == CameraStyle.Combat);
        FreeLookCam.SetActive(newStyle == CameraStyle.basic);

        currentstyle = newStyle;
    }
}
