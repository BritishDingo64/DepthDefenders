using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WaveUpgradeManager : MonoBehaviour
{
    const float ChoiceCooldownSeconds = 1f;
    const float StatUpgradeStep = 1.1f;
    const float GoldUpgradeStep = 1.15f;
    const float CritChanceStep = 0.1f;

    enum UpgradeKind
    {
        PlayerHealth,
        PlayerDamage,
        TowerDamage,
        PlayerFireRate,
        TowerFireRate,
        MovementSpeed,
        HealingAndLifesteal,
        CrystalHealth,
        BarricadeHealth,
        EnemyGold,
        CriticalChance
    }

    readonly struct UpgradeOption
    {
        public readonly UpgradeKind Kind;
        public readonly string Title;
        public readonly string Description;

        public UpgradeOption(UpgradeKind kind, string title, string description)
        {
            Kind = kind;
            Title = title;
            Description = description;
        }
    }

    static WaveUpgradeManager instance;

    public static float PlayerHealthMultiplier { get; private set; } = 1f;
    public static float PlayerDamageMultiplier { get; private set; } = 1f;
    public static float PlayerFireRateMultiplier { get; private set; } = 1f;
    public static float TowerDamageMultiplier { get; private set; } = 1f;
    public static float TowerFireRateMultiplier { get; private set; } = 1f;
    public static float MovementSpeedMultiplier { get; private set; } = 1f;
    public static float HealingReceivedMultiplier { get; private set; } = 1f;
    public static float LifestealPercent { get; private set; } = 0f;
    public static float CrystalHealthMultiplier { get; private set; } = 1f;
    public static float BarricadeHealthMultiplier { get; private set; } = 1f;
    public static float EnemyGoldMultiplier { get; private set; } = 1f;
    public static float CritChance { get; private set; } = 0f;
    public static bool IsSelectionActive => instance != null && instance.isSelectionActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        PlayerHealthMultiplier = 1f;
        PlayerDamageMultiplier = 1f;
        PlayerFireRateMultiplier = 1f;
        TowerDamageMultiplier = 1f;
        TowerFireRateMultiplier = 1f;
        MovementSpeedMultiplier = 1f;
        HealingReceivedMultiplier = 1f;
        LifestealPercent = 0f;
        CrystalHealthMultiplier = 1f;
        BarricadeHealthMultiplier = 1f;
        EnemyGoldMultiplier = 1f;
        CritChance = 0f;
    }

    static WaveUpgradeManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<WaveUpgradeManager>();
            if (instance != null)
            {
                return instance;
            }

            GameObject managerObject = new GameObject(nameof(WaveUpgradeManager));
            instance = managerObject.AddComponent<WaveUpgradeManager>();
            return instance;
        }
    }

    GameObject canvasRoot;
    Canvas canvas;
    TextMeshProUGUI titleText;
    Button[] optionButtons = new Button[3];
    TextMeshProUGUI[] optionLabels = new TextMeshProUGUI[3];
    readonly List<UpgradeOption> currentChoices = new List<UpgradeOption>(3);
    bool isSelectionActive;
    bool canChooseUpgrade;
    float previousTimeScale = 1f;
    CursorLockMode previousLockState = CursorLockMode.Locked;
    bool previousCursorVisible;
    Coroutine enableChoicesRoutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureUiCreated();
        canvasRoot.SetActive(false);
    }

    public static void ResetRunState()
    {
        PlayerHealthMultiplier = 1f;
        PlayerDamageMultiplier = 1f;
        PlayerFireRateMultiplier = 1f;
        TowerDamageMultiplier = 1f;
        TowerFireRateMultiplier = 1f;
        MovementSpeedMultiplier = 1f;
        HealingReceivedMultiplier = 1f;
        LifestealPercent = 0f;
        CrystalHealthMultiplier = 1f;
        BarricadeHealthMultiplier = 1f;
        EnemyGoldMultiplier = 1f;
        CritChance = 0f;
        Time.timeScale = 1f;

        if (instance != null)
        {
            instance.CloseSelection(false);
        }
    }

    public static void BeginWaveUpgradeSelection(Crystal crystal, int waveNumber)
    {
        Instance.OpenSelection(crystal, waveNumber);
    }

    public static float ApplyCriticalHit(float damage, out bool wasCritical)
    {
        if (damage <= 0f)
        {
            wasCritical = false;
            return 0f;
        }

        if (CritChance <= 0f || UnityEngine.Random.value > Mathf.Clamp01(CritChance))
        {
            wasCritical = false;
            return damage;
        }

        wasCritical = true;
        return damage * 2f;
    }

    void EnsureUiCreated()
    {
        if (canvasRoot != null)
        {
            return;
        }

        canvasRoot = new GameObject("Wave Upgrade Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform, false);

        canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler canvasScaler = canvasRoot.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreateUiObject("Panel", canvasRoot.transform);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 480f);
        panelRect.anchoredPosition = Vector2.zero;

        GameObject titleObject = CreateUiObject("Title", panel.transform);
        titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 40f;
        titleText.color = Color.white;
        titleText.text = "Choose an upgrade";

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);

        optionButtons[0] = CreateButton(panel.transform, 0, "Option 1", new Vector2(0f, 120f));
        optionButtons[1] = CreateButton(panel.transform, 1, "Option 2", new Vector2(0f, 0f));
        optionButtons[2] = CreateButton(panel.transform, 2, "Option 3", new Vector2(0f, -120f));

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int optionIndex = i;
            optionButtons[i].onClick.AddListener(() => SelectChoice(optionIndex));
        }

        EnsureEventSystemExists();
    }

    GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    Button CreateButton(Transform parent, int index, string objectName, Vector2 anchoredPosition)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.18f, 0.28f, 0.42f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.28f, 0.42f, 1f);
        colors.highlightedColor = new Color(0.26f, 0.4f, 0.58f, 1f);
        colors.pressedColor = new Color(0.12f, 0.2f, 0.3f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(520f, 88f);
        rectTransform.anchoredPosition = anchoredPosition;

        GameObject labelObject = CreateUiObject("Label", buttonObject.transform);
        TextMeshProUGUI buttonLabel = labelObject.AddComponent<TextMeshProUGUI>();
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.fontSize = 28f;
        buttonLabel.color = Color.white;
        optionLabels[index] = buttonLabel;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(18f, 10f);
        labelRect.offsetMax = new Vector2(-18f, -10f);

        return button;
    }

    void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.SetParent(transform, false);
    }

    void OpenSelection(Crystal crystal, int waveNumber)
    {
        if (isSelectionActive)
        {
            return;
        }

        EnsureUiCreated();
        BuildRandomChoices();

        isSelectionActive = true;
        canChooseUpgrade = false;
        previousTimeScale = Time.timeScale;
        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (titleText != null)
        {
            titleText.text = $"Wave {waveNumber} complete - choose an upgrade";
        }

        canvasRoot.SetActive(true);
        RefreshChoiceButtons();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (enableChoicesRoutine != null)
        {
            StopCoroutine(enableChoicesRoutine);
        }

        enableChoicesRoutine = StartCoroutine(EnableChoicesAfterDelay());
    }

    void BuildRandomChoices()
    {
        currentChoices.Clear();

        List<UpgradeOption> pool = new List<UpgradeOption>(GetAllUpgradeOptions());
        while (currentChoices.Count < 3 && pool.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            currentChoices.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    IEnumerable<UpgradeOption> GetAllUpgradeOptions()
    {
        yield return new UpgradeOption(UpgradeKind.PlayerHealth, "+10% Player Max Health", "More survivability");
        yield return new UpgradeOption(UpgradeKind.PlayerDamage, "+10% Player Damage", "Stronger melee attacks");
        yield return new UpgradeOption(UpgradeKind.TowerDamage, "+10% Tower Damage", "All non-barricade towers hit harder");
        yield return new UpgradeOption(UpgradeKind.PlayerFireRate, "+10% Player Fire Rate", "Attack faster");
        yield return new UpgradeOption(UpgradeKind.TowerFireRate, "+10% Tower Fire Rate", "All non-barricade towers fire faster");
        yield return new UpgradeOption(UpgradeKind.MovementSpeed, "+10% Movement Speed", "Move and reposition faster");
        yield return new UpgradeOption(UpgradeKind.HealingAndLifesteal, "+10% Healing / Lifesteal", "Receive more healing and restore health on hits");
        yield return new UpgradeOption(UpgradeKind.CrystalHealth, "+10% Crystal Health", "Make the objective harder to break");
        yield return new UpgradeOption(UpgradeKind.BarricadeHealth, "+10% Barricade Health", "Make barricades sturdier");
        yield return new UpgradeOption(UpgradeKind.EnemyGold, "+15% Gold Earned", "Enemies drop more gold");
        yield return new UpgradeOption(UpgradeKind.CriticalChance, "Attacks Can Crit", "Gain a chance for attacks to deal 2x damage");
    }

    void RefreshChoiceButtons()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null)
            {
                continue;
            }

            bool hasChoice = i < currentChoices.Count;
            optionButtons[i].gameObject.SetActive(hasChoice);
            optionButtons[i].interactable = hasChoice && canChooseUpgrade;
            if (!hasChoice)
            {
                continue;
            }

            TextMeshProUGUI label = optionLabels[i];
            if (label == null)
            {
                label = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                optionLabels[i] = label;
            }

            if (label != null)
            {
                label.text = $"{currentChoices[i].Title}\n<size=70%>{currentChoices[i].Description}</size>";
            }
        }
    }

    void SelectChoice(int index)
    {
        if (!isSelectionActive || !canChooseUpgrade)
        {
            return;
        }

        if (index < 0 || index >= currentChoices.Count)
        {
            return;
        }

        ApplyChoice(currentChoices[index]);
        RefreshSceneMultipliers();
        CloseSelection(true);
    }

    void ApplyChoice(UpgradeOption option)
    {
        switch (option.Kind)
        {
            case UpgradeKind.PlayerHealth:
                PlayerHealthMultiplier *= StatUpgradeStep;
                break;
            case UpgradeKind.PlayerDamage:
                PlayerDamageMultiplier *= StatUpgradeStep;
                break;
            case UpgradeKind.TowerDamage:
                TowerDamageMultiplier *= StatUpgradeStep;
                break;
            case UpgradeKind.PlayerFireRate:
                PlayerFireRateMultiplier *= StatUpgradeStep;
                break;
            case UpgradeKind.TowerFireRate:
                TowerFireRateMultiplier *= StatUpgradeStep;
                break;
            case UpgradeKind.MovementSpeed:
                MovementSpeedMultiplier *= StatUpgradeStep;
                break;
            case UpgradeKind.HealingAndLifesteal:
                HealingReceivedMultiplier *= StatUpgradeStep;
                LifestealPercent = Mathf.Clamp01(LifestealPercent + 0.1f);
                break;
            case UpgradeKind.CrystalHealth:
                CrystalHealthMultiplier *= StatUpgradeStep;
                break;
            case UpgradeKind.BarricadeHealth:
                BarricadeHealthMultiplier *= StatUpgradeStep;
                break;
            case UpgradeKind.EnemyGold:
                EnemyGoldMultiplier *= GoldUpgradeStep;
                break;
            case UpgradeKind.CriticalChance:
                CritChance = Mathf.Clamp01(CritChance + CritChanceStep);
                break;
        }
    }

    void RefreshSceneMultipliers()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ApplyHealthMultiplier(PlayerHealthMultiplier);
        }

        PlayerAttack playerAttack = FindFirstObjectByType<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.ApplyDamageMultiplier(PlayerDamageMultiplier);
            playerAttack.ApplyFireRateMultiplier(PlayerFireRateMultiplier);
            playerAttack.ApplyLifestealPercent(LifestealPercent);
        }

        Movement movement = FindFirstObjectByType<Movement>();
        if (movement != null)
        {
            movement.ApplyMovementMultiplier(MovementSpeedMultiplier);
        }

        Crystal crystal = FindFirstObjectByType<Crystal>();
        if (crystal != null)
        {
            crystal.ApplyHealthMultiplier(CrystalHealthMultiplier);
        }

        BarricadeDefenseTower[] barricades = FindObjectsByType<BarricadeDefenseTower>(FindObjectsSortMode.None);
        for (int i = 0; i < barricades.Length; i++)
        {
            if (barricades[i] != null)
            {
                barricades[i].ApplyHealthMultiplier(BarricadeHealthMultiplier);
            }
        }

        BubbleMortarTower[] bubbleMortarTowers = FindObjectsByType<BubbleMortarTower>(FindObjectsSortMode.None);
        for (int i = 0; i < bubbleMortarTowers.Length; i++)
        {
            if (bubbleMortarTowers[i] != null)
            {
                bubbleMortarTowers[i].ApplyDamageMultiplier(TowerDamageMultiplier);
                bubbleMortarTowers[i].ApplyFireRateMultiplier(TowerFireRateMultiplier);
            }
        }

        IceTower[] iceTowers = FindObjectsByType<IceTower>(FindObjectsSortMode.None);
        for (int i = 0; i < iceTowers.Length; i++)
        {
            if (iceTowers[i] != null)
            {
                iceTowers[i].ApplyDamageMultiplier(TowerDamageMultiplier);
                iceTowers[i].ApplyFireRateMultiplier(TowerFireRateMultiplier);
            }
        }

        TeslaChainTower[] teslaTowers = FindObjectsByType<TeslaChainTower>(FindObjectsSortMode.None);
        for (int i = 0; i < teslaTowers.Length; i++)
        {
            if (teslaTowers[i] != null)
            {
                teslaTowers[i].ApplyDamageMultiplier(TowerDamageMultiplier);
                teslaTowers[i].ApplyFireRateMultiplier(TowerFireRateMultiplier);
            }
        }
    }

    void CloseSelection(bool resumeGame)
    {
        if (!isSelectionActive)
        {
            return;
        }

        isSelectionActive = false;
        canChooseUpgrade = false;

        if (enableChoicesRoutine != null)
        {
            StopCoroutine(enableChoicesRoutine);
            enableChoicesRoutine = null;
        }

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(false);
        }

        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;
    }

    System.Collections.IEnumerator EnableChoicesAfterDelay()
    {
        yield return new WaitForSecondsRealtime(ChoiceCooldownSeconds);

        if (!isSelectionActive)
        {
            enableChoicesRoutine = null;
            yield break;
        }

        canChooseUpgrade = true;
        RefreshChoiceButtons();

        if (optionButtons[0] != null)
        {
            optionButtons[0].Select();
        }

        enableChoicesRoutine = null;
    }
}