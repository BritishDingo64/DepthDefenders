using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject gameOverCanvas;
    [SerializeField] TMP_Text enemiesKilledText;
    [SerializeField] TMP_Text wavesSurvivedText;
    [SerializeField] Button returnToMenuButton;

    [Header("Scene")]
    [SerializeField] string mainMenuSceneName = "menu";

    void Awake()
    {
        if (gameOverCanvas == null)
            gameOverCanvas = gameObject;

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
            gameOverCanvas.SetActive(true);

        RefreshStats();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void RefreshStats()
    {
        if (enemiesKilledText != null)
            enemiesKilledText.text = RunStats.EnemiesKilled.ToString();

        if (wavesSurvivedText != null)
            wavesSurvivedText.text = RunStats.WavesSurvived.ToString();
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
}