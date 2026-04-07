using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class Crystal : MonoBehaviour {
    public bool waveStarted;
    public int waveNumber;

    [Header("References")]
    [SerializeField]
    GameObject orientation;
    [SerializeField]
    List<Spawner> spawners = new();

    [Header("Crystal Health")]
    [SerializeField]
    float maxHealth = 500f;
    [SerializeField]
    float currentHealth;

    [Header("Input")]
    [SerializeField]
    KeyCode startWaveInteractKey = KeyCode.E;
    [SerializeField]
    KeyCode startWaveRemoteKey = KeyCode.X;
    [SerializeField]
    float interactDistance = 2f;

    [Header("UI")]
    [SerializeField]
    TMP_Text statusText;
    [SerializeField]
    TMP_Text phaseText;
    [SerializeField]
    TMP_Text waveInfoText;
    [SerializeField]
    TMP_Text crystalHealthText;
    [SerializeField]
    GameObject gameOverUI;

    [Header("Mode Objects")]
    [SerializeField]
    GameObject buildModeRoot;
    [SerializeField]
    GameObject combatModeRoot;

    [Header("Phase Text Animation")]
    [SerializeField]
    float phaseFadeDuration = 0.35f;
    [SerializeField]
    float phaseVisibleDuration = 2f;
    [Header("Debug")]
    [SerializeField]
    bool debugLogs;

    bool isGameOver;
    string temporaryStatusMessage;
    float temporaryStatusUntil;
    string lastPhaseLabel;
    Coroutine phaseTextFadeRoutine;

    // crystal is the target for the monsters, if the monsters reach the crystal they attack it
    // once the crystal's health reaches 0, the player loses and the game is over
    // the player can start a wave of monsters by looking at the crystal and pressing the "e" key, this will trigger the monsters to start spawning and attacking the crystal
    // the player can also start a wave by pressing the "x" key, this way the player can remotely start the wave
    void Start() {
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);

        EnsureSpawnersAssigned();

        SetPhase(buildingPhase: true);

        if (phaseText != null) {
            SetPhaseTextAlpha(0f);
        }

        UpdateUI();
    }

    // Update is called once per frame
    void Update() {
        if (isGameOver) {
            return;
        }

        HasPlayerStartedWave();
        TryFinishWave();
        UpdateUI();
    }

    void HasPlayerStartedWave() {
        if (!waveStarted) {
            bool lookingAtCrystal = IsPlayerLookingAt();

            if (lookingAtCrystal) {
                SetTemporaryStatus($"Press {startWaveInteractKey} to start wave | Press {startWaveRemoteKey} to start remotely", 0.1f);
            }

            if (Input.GetKeyDown(startWaveRemoteKey)) {
                StartNextWave();
                return;
            }

            if (lookingAtCrystal && Input.GetKeyDown(startWaveInteractKey)) {
                StartNextWave();
            }
        }
    }

    bool IsPlayerLookingAt() {
        if (orientation == null) {
            if (Camera.main != null) {
                orientation = Camera.main.gameObject;
            }
            else {
                return false;
            }
        }

        Ray raycast = new Ray(orientation.transform.position, orientation.transform.forward);
        if (!Physics.Raycast(raycast.origin, raycast.direction, out RaycastHit hitInfo, interactDistance)) {
            return false;
        }

        return hitInfo.transform == transform || hitInfo.transform.IsChildOf(transform);
    }

    void StartNextWave() {
        if (isGameOver || waveStarted) return;

        waveStarted = true;
        waveNumber++;
        SetPhase(buildingPhase: false);
        DisplayText($"Combat phase - wave {waveNumber} started");
    }

    void TryFinishWave() {
        if (!waveStarted) return;

        bool anySpawnerStillPendingOrActive = false;
        for (int i = 0; i < spawners.Count; i++) {
            Spawner spawner = spawners[i];
            if (spawner == null) continue;

            if (spawner.HasPendingOrActiveWave(waveNumber)) {
                anySpawnerStillPendingOrActive = true;
                break;
            }
        }

        if (!anySpawnerStillPendingOrActive && Spawner.ActiveEnemyCount <= 0) {
            waveStarted = false;
            SetPhase(buildingPhase: true);
            DisplayText($"Wave {waveNumber} ended - building phase");
        }
    }

    void EnsureSpawnersAssigned() {
        if (spawners == null) {
            spawners = new List<Spawner>();
        }

        spawners.RemoveAll(x => x == null);

        if (spawners.Count == 0) {
            Spawner[] foundSpawners = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
            for (int i = 0; i < foundSpawners.Length; i++) {
                spawners.Add(foundSpawners[i]);
            }
        }

        for (int i = 0; i < spawners.Count; i++) {
            if (spawners[i] != null && spawners[i].crystal == null) {
                spawners[i].crystal = gameObject;
            }
        }
    }

    public void TakeDamage(float amount) {
        if (isGameOver) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);

        if (currentHealth <= 0f) {
            HandleGameOver();
        }
    }

    void HandleGameOver() {
        isGameOver = true;
        waveStarted = false;
        DisplayText("Game over - the crystal was destroyed");

        if (gameOverUI != null) {
            gameOverUI.SetActive(true);
        }

        if (buildModeRoot != null) buildModeRoot.SetActive(false);
        if (combatModeRoot != null) combatModeRoot.SetActive(false);
    }

    void DisplayText() {
        DisplayText(null);
    }

    void DisplayText(string message) {
        // add a inspector assigned text mesh pro or something to display the text on the screen that will say things like, combat phase, wave started, wave ended and building phase.
        //during the building phases the player can build and has the build camera mode. and during the combat phase the player has the combat camera mode and can attack monsters and has a health bar. the player can also see the wave number and how many monsters are left in the wave.
        if (!string.IsNullOrWhiteSpace(message)) {
            SetTemporaryStatus(message, 2f);
            if (debugLogs) {
                Debug.Log(message);
            }
        }

        UpdateUI();
    }

    void SetPhase(bool buildingPhase) {
        if (buildModeRoot != null) buildModeRoot.SetActive(buildingPhase);
        if (combatModeRoot != null) combatModeRoot.SetActive(!buildingPhase);
    }

    void SetTemporaryStatus(string message, float durationSeconds) {
        temporaryStatusMessage = message;
        temporaryStatusUntil = Time.time + Mathf.Max(0.01f, durationSeconds);
    }

    void UpdateUI() {
        string phaseLabel = isGameOver ? "Game Over" : (waveStarted ? "Combat Phase" : "Building Phase");
        UpdatePhaseLabel(phaseLabel);

        if (waveInfoText != null) {
            waveInfoText.text = $"Wave: {waveNumber}  |  Monsters Left: {Spawner.ActiveEnemyCount}";
        }

        if (crystalHealthText != null) {
            crystalHealthText.text = $"Crystal HP: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }

        if (statusText != null) {
            if (!string.IsNullOrWhiteSpace(temporaryStatusMessage) && Time.time <= temporaryStatusUntil) {
                statusText.text = temporaryStatusMessage;
            }
            else {
                temporaryStatusMessage = string.Empty;
                statusText.text = waveStarted
                    ? "Defend the crystal!"
                    : $"Look at crystal + {startWaveInteractKey} (or press {startWaveRemoteKey}) to start next wave";
            }
        }
    }

    void UpdatePhaseLabel(string newLabel) {
        if (phaseText == null) return;

        if (newLabel == lastPhaseLabel) return;
        lastPhaseLabel = newLabel;

        if (phaseTextFadeRoutine != null) {
            StopCoroutine(phaseTextFadeRoutine);
        }

        phaseTextFadeRoutine = StartCoroutine(AnimatePhaseLabel(newLabel));
    }

    IEnumerator AnimatePhaseLabel(string label) {
        phaseText.text = label;

        float fadeDuration = Mathf.Max(0.01f, phaseFadeDuration);
        float visibleDuration = Mathf.Max(0f, phaseVisibleDuration);

        for (float t = 0f; t < fadeDuration; t += Time.deltaTime) {
            SetPhaseTextAlpha(t / fadeDuration);
            yield return null;
        }
        SetPhaseTextAlpha(1f);

        if (visibleDuration > 0f) {
            yield return new WaitForSeconds(visibleDuration);
        }

        for (float t = 0f; t < fadeDuration; t += Time.deltaTime) {
            SetPhaseTextAlpha(1f - (t / fadeDuration));
            yield return null;
        }
        SetPhaseTextAlpha(0f);

        phaseTextFadeRoutine = null;
    }

    void SetPhaseTextAlpha(float alpha) {
        if (phaseText == null) return;
        Color c = phaseText.color;
        c.a = Mathf.Clamp01(alpha);
        phaseText.color = c;
    }
}