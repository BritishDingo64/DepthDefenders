using UnityEngine;

public class Crystal : MonoBehaviour {
    public bool waveStarted;
    public int waveNumber;
    [SerializeField]
    GameObject orientation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        HasPlayerStartedWave();
    }
    void HasPlayerStartedWave() {
        if (!waveStarted) {
            IsPlayerLookingAt();
            if (Input.GetKeyDown("x")) {
                DisplayText();
                waveStarted = true;
                waveNumber++;
            }
        }
    }
    void IsPlayerLookingAt() {
        Ray raycast = new Ray(orientation.transform.position, orientation.transform.forward);
        if (Physics.Raycast(raycast.origin, raycast.direction, out RaycastHit hitInfo, 2)) {
            if (hitInfo.transform.name == "Crystal") {
                if (Input.GetKeyDown("e") && !waveStarted) {
                    waveStarted = true;
                    waveNumber++;
                }
            }
        }
    }
    void DisplayText() {
        Debug.LogWarning("not implemented");
    }
}