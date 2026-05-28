using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Menu Canvas")]
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Selection Arrow")]
    [SerializeField] private RectTransform selectionArrow;
    [SerializeField] private Vector2 arrowOffset = new Vector2(-40f, 0f);
    [SerializeField, Min(0f)] private float idleSwayAmplitude = 4f;
    [SerializeField, Min(0f)] private float idleSwaySpeed = 3f;
    [SerializeField] private Vector2 clickSlideOffset = new Vector2(-14f, 0f);
    [SerializeField, Min(0.01f)] private float clickSlideDuration = 0.12f;

    [SerializeField] private string gameSceneName = "game";
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.5f;

    private Button[] menuButtons;
    private Button highlightedButton;
    private Button focusedButton;
    private float clickSlideAmount;
    private Coroutine clickRoutine;
    private SceneTransitionRunner transitionRunner;

    private void Awake()
    {
        menuButtons = new[] { startButton, quitButton };
        SetupButtonCallbacks(startButton);
        SetupButtonCallbacks(quitButton);
        HookButtonClick(startButton, StartGame);
        HookButtonClick(quitButton, QuitGame);
        transitionRunner = SceneTransitionRunner.Create(gameSceneName, fadeDuration);
    }

    private void Start()
    {
        if (EventSystem.current != null && startButton != null)
        {
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }

        UpdateSelectionArrow();
    }

    private void Update()
    {
        UpdateSelectionArrow();
    }

    public void StartGame()
    {
        BeginClickTransition(startButton, true);
    }

    public void QuitGame()
    {
        BeginClickTransition(quitButton, false);
    }

    private void BeginClickTransition(Button clickedButton, bool loadGameScene)
    {
        if (clickedButton != null)
            focusedButton = clickedButton;

        if (clickRoutine != null)
            StopCoroutine(clickRoutine);

        clickRoutine = StartCoroutine(ClickTransitionRoutine(loadGameScene));
    }

    private IEnumerator ClickTransitionRoutine(bool loadGameScene)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, clickSlideDuration);
        float startOffset = clickSlideAmount;
        float targetOffset = clickSlideOffset.x;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            clickSlideAmount = Mathf.Lerp(startOffset, targetOffset, elapsed / duration);
            yield return null;
        }

        clickSlideAmount = targetOffset;

        if (loadGameScene)
        {
            if (transitionRunner != null)
                transitionRunner.LoadSceneWithFade();
        }
        else
        {
            if (transitionRunner != null)
                transitionRunner.QuitWithFade();
        }
    }

    private void HookButtonClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void UpdateSelectionArrow()
    {
        if (selectionArrow == null || EventSystem.current == null)
            return;

        Button selectedButton = highlightedButton != null
            ? highlightedButton
            : GetMatchingButton(EventSystem.current.currentSelectedGameObject);

        if (selectedButton == null)
        {
            selectionArrow.gameObject.SetActive(false);
            return;
        }

        focusedButton = selectedButton;

        RectTransform selectedRect = selectedButton.GetComponent<RectTransform>();
        if (selectedRect == null)
            return;

        Vector3 sway = new Vector3(Mathf.Sin(Time.unscaledTime * idleSwaySpeed) * idleSwayAmplitude, 0f, 0f);
        Vector3 clickSlide = new Vector3(clickSlideAmount, 0f, 0f);
        selectionArrow.position = selectedRect.position + (Vector3)arrowOffset + sway + clickSlide;
        selectionArrow.gameObject.SetActive(true);
    }

    private void SetupButtonCallbacks(Button button)
    {
        if (button == null)
            return;

        EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();

        AddTriggerEntry(eventTrigger, EventTriggerType.PointerEnter, () => highlightedButton = button);
        AddTriggerEntry(eventTrigger, EventTriggerType.Select, () => highlightedButton = button);
        AddTriggerEntry(eventTrigger, EventTriggerType.PointerExit, ClearHighlightedButton);
        AddTriggerEntry(eventTrigger, EventTriggerType.Deselect, ClearHighlightedButton);
    }

    private void AddTriggerEntry(EventTrigger eventTrigger, EventTriggerType triggerType, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = triggerType
        };

        entry.callback.AddListener(_ => action());
        eventTrigger.triggers.Add(entry);
    }

    private void ClearHighlightedButton()
    {
        if (EventSystem.current != null)
        {
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            highlightedButton = GetMatchingButton(selectedObject);
            return;
        }

        highlightedButton = null;
    }

    private Button GetMatchingButton(GameObject selectedObject)
    {
        if (selectedObject == null || menuButtons == null)
            return null;

        for (int i = 0; i < menuButtons.Length; i++)
        {
            Button button = menuButtons[i];
            if (button != null && selectedObject == button.gameObject)
                return button;
        }

        return null;
    }

    private class SceneTransitionRunner : MonoBehaviour
    {
        private static SceneTransitionRunner instance;

        private string targetSceneName;
        private float transitionFadeDuration;
        private CanvasGroup fadeOverlay;

        public static SceneTransitionRunner Create(string sceneName, float fadeDuration)
        {
            if (instance == null)
            {
                GameObject runnerObject = new GameObject("SceneTransitionRunner");
                DontDestroyOnLoad(runnerObject);
                instance = runnerObject.AddComponent<SceneTransitionRunner>();
                instance.BuildOverlay();
            }

            instance.targetSceneName = sceneName;
            instance.transitionFadeDuration = fadeDuration;
            return instance;
        }

        public void LoadSceneWithFade()
        {
            StopAllCoroutines();
            StartCoroutine(LoadSceneRoutine());
        }

        public void QuitWithFade()
        {
            StopAllCoroutines();
            StartCoroutine(QuitRoutine());
        }

        private void BuildOverlay()
        {
            GameObject overlayObject = new GameObject("FadeOverlay");
            overlayObject.transform.SetParent(transform, false);

            Canvas canvas = overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            overlayObject.AddComponent<CanvasScaler>();
            overlayObject.AddComponent<GraphicRaycaster>();

            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(overlayObject.transform, false);

            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = imageObject.AddComponent<Image>();
            image.color = Color.black;

            fadeOverlay = overlayObject.AddComponent<CanvasGroup>();
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
            overlayObject.SetActive(false);
        }

        private IEnumerator LoadSceneRoutine()
        {
            yield return FadeTo(1f);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
            if (loadOperation != null)
            {
                while (loadOperation.progress < 0.9f)
                    yield return null;

                loadOperation.allowSceneActivation = true;

                while (!loadOperation.isDone)
                    yield return null;
            }

            yield return null;
            yield return FadeTo(0f);
        }

        private IEnumerator QuitRoutine()
        {
            yield return FadeTo(1f);
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (fadeOverlay == null)
                yield break;

            fadeOverlay.gameObject.SetActive(true);

            float startAlpha = fadeOverlay.alpha;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, transitionFadeDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            fadeOverlay.alpha = targetAlpha;

            if (Mathf.Approximately(targetAlpha, 0f))
                fadeOverlay.gameObject.SetActive(false);
        }
    }
}
