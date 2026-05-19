using UnityEngine;
using TMPro;

public class Currency : MonoBehaviour
{
    public static Currency Instance { get; private set; }

    [Header("Money")]
    [Min(0)]
    [SerializeField] int startingMoney = 150;
    [Min(0)]
    [SerializeField] int currentMoney;

    [Header("UI")]
    [SerializeField] TMP_Text currencyText;
    [SerializeField] string currencyLabel = "Money";

    public int CurrentMoney => currentMoney;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentMoney = Mathf.Max(0, currentMoney);

        if (currentMoney <= 0 && startingMoney > 0)
        {
            currentMoney = startingMoney;
        }

        RefreshCurrencyText();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        RefreshCurrencyText();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (currentMoney < amount) return false;

        currentMoney -= amount;
        RefreshCurrencyText();
        return true;
    }

    public void SetCurrencyText(TMP_Text text)
    {
        currencyText = text;
        RefreshCurrencyText();
    }

    void RefreshCurrencyText()
    {
        if (currencyText == null) return;
        currencyText.text = $"{currencyLabel}: {currentMoney}";
    }

    public static bool TryAddMoney(int amount)
    {
        if (amount <= 0) return false;

        Currency target = Instance;
        if (target == null)
        {
            target = FindFirstObjectByType<Currency>();
        }

        if (target == null)
        {
            Debug.LogWarning("No Currency component found in the scene. Enemy reward was not added.");
            return false;
        }

        target.AddMoney(amount);
        return true;
    }

    public static bool TrySpend(int amount)
    {
        if (amount <= 0) return true;

        Currency target = Instance;
        if (target == null)
        {
            target = FindFirstObjectByType<Currency>();
        }

        if (target == null)
        {
            Debug.LogWarning("No Currency component found in the scene. Could not spend money.");
            return false;
        }

        return target.TrySpendMoney(amount);
    }

    public static bool TryGetCurrentMoney(out int amount)
    {
        Currency target = Instance;
        if (target == null)
        {
            target = FindFirstObjectByType<Currency>();
        }

        if (target == null)
        {
            amount = 0;
            return false;
        }

        amount = target.CurrentMoney;
        return true;
    }
}
