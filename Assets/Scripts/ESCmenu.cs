using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ESCmenu : MonoBehaviour
{
    [Header("Menu UI")]
    [SerializeField] private GameObject pauseCanvas;

    [Header("Buttons")]
    [SerializeField] private Button quitButton;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button backToGameButton;

    [Header("Audio")]
    [SerializeField] private Slider musicVolumeSlider;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "menu";

    private bool isOpen;

    private void Awake()
    {
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }

        if (backToGameButton != null)
        {
            backToGameButton.onClick.AddListener(CloseMenu);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = AudioListener.volume;
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
    }

    private void OnDestroy()
    {
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
        }

        if (backToGameButton != null)
        {
            backToGameButton.onClick.RemoveListener(CloseMenu);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    public void OpenMenu()
    {
        isOpen = true;

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(true);
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        isOpen = false;

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetMusicVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }
}
