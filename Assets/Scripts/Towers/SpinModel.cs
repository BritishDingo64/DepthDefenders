using UnityEngine;

// Simple helper that continuously rotates the attached object around its Y axis.
public class SpinModel : MonoBehaviour
{
    public float rotationSpeed = 50f;
    
    void Update()
    {
        // Rotate each frame to create a spinning visual effect.
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}