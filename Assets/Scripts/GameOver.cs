using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    public static GameOver Instance { get; private set; }
    public static bool IsActive => Instance != null && Instance.IsVisible();

    [Header("UI")]
    [SerializeField] GameObject gameOverCanvas;
    [SerializeField] TMP_Text enemiesKilledText;
    [SerializeField] TMP_Text wavesSurvivedText;
    [SerializeField] Button returnToMenuButton;

    [Header("Scene")]
    [SerializeField] string mainMenuSceneName = "menu";

    void Awake()
    {
        Instance = this;

        if (gameOverCanvas == null)
            gameOverCanvas = gameObject;

        AutoBindStatsTexts();

        if (returnToMenuButton == null)
            returnToMenuButton = GetComponentInChildren<Button>(true);

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        HideUI();
    }

    public void ShowGameOver()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            gameOverCanvas.transform.SetAsLastSibling();
        }

        Debug.Log($"GameOver shown. Enemies killed: {RunStats.EnemiesKilled}, Waves survived: {RunStats.WavesSurvived}");
        RefreshStats();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (IsVisible())
        {
            RefreshStats();
        }
    }

    void OnEnable()
    {
        RefreshStats();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public static void ShowGameOverIfAvailable()
    {
        if (Instance != null)
        {
            Instance.ShowGameOver();
            return;
        }

        GameOver fallback = FindFirstObjectByType<GameOver>(FindObjectsInactive.Include);
        if (fallback != null)
        {
            fallback.ShowGameOver();
        }
    }

    void RefreshStats()
    {
        AutoBindStatsTexts();

        if (enemiesKilledText != null)
            enemiesKilledText.text = RunStats.EnemiesKilled.ToString();

        if (wavesSurvivedText != null)
            wavesSurvivedText.text = RunStats.WavesSurvived.ToString();
    }

    void AutoBindStatsTexts()
    {
        if (enemiesKilledText != null && wavesSurvivedText != null)
            return;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts == null || texts.Length == 0)
            return;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            string lowerName = text.name.ToLowerInvariant();
            string lowerText = text.text != null ? text.text.ToLowerInvariant() : string.Empty;

            if (enemiesKilledText == null && (lowerName.Contains("kill") || lowerText.Contains("kill") || lowerName.Contains("enemy")))
            {
                enemiesKilledText = text;
                continue;
            }

            if (wavesSurvivedText == null && (lowerName.Contains("wave") || lowerText.Contains("wave")))
            {
                wavesSurvivedText = text;
            }
        }

        if (enemiesKilledText == null || wavesSurvivedText == null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;

                if (enemiesKilledText == null)
                {
                    enemiesKilledText = text;
                    continue;
                }

                if (wavesSurvivedText == null && text != enemiesKilledText)
                {
                    wavesSurvivedText = text;
                    break;
                }
            }
        }
    }

    void HideUI()
    {
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
    }

    bool IsVisible()
    {
        return gameOverCanvas != null && gameOverCanvas.activeSelf;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}