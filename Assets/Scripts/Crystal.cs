using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

// Controls the crystal as the central objective for survival waves.
// It tracks wave state, player input for starting waves, crystal health, UI updates, and game-over state.
public class Crystal : MonoBehaviour {
    // Whether a wave is currently active.
    public bool waveStarted;
    // Current wave number completed/active.
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

    void Start() {
        // Initialize crystal health and clamp to range.
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);

        // Ensure any spawners in the scene are linked to this crystal.
        EnsureSpawnersAssigned();

        // Start in building phase before waves begin.
        SetPhase(buildingPhase: true);

        if (phaseText != null) {
            SetPhaseTextAlpha(0f);
        }

        // Refresh UI at the beginning.
        UpdateUI();
    }

    void Update() {
        if (isGameOver) {
            return;
        }

        // Check whether the player has requested the next wave.
        HasPlayerStartedWave();

        // See if the current wave is complete and switch back to building phase.
        TryFinishWave();

        // Keep UI updated each frame.
        UpdateUI();
    }

    void HasPlayerStartedWave() {
        if (!waveStarted) {
            bool lookingAtCrystal = IsPlayerLookingAt();

            // Show prompt when player looks at the crystal.
            if (lookingAtCrystal) {
                SetTemporaryStatus($"Press {startWaveInteractKey} to start wave | Press {startWaveRemoteKey} to start remotely", 0.1f);
            }

            // Remote wave start input.
            if (Input.GetKeyDown(startWaveRemoteKey)) {
                StartNextWave();
                return;
            }

            // Direct interact input when looking at the crystal.
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

        // Raycast from the player's orientation forward to determine if the player is looking
        // at the crystal (within `interactDistance`). This allows interaction prompts only
        // when the player is intentionally looking at the crystal object or one of its children.
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
        // Transition from building into combat for the new wave and notify the UI/logs.
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

        // If all spawners have finished issuing their enemies and no active enemies remain,
        // end the combat phase and return to building mode.
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

        // Ensure each spawner knows which crystal GameObject to reference for wave control.
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
        // If a message is provided, show it temporarily and optionally log it.
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

        // Update the status text to show temporary messages (prompts) or the default instruction.
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