using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCurrencyView : BaseMonoBehaviour
{
    private const int TestCoinAmount = 100;
    private const int TestDiamondAmount = 10;

    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text diamondsText;
    [SerializeField] private Button addCoinsButton;
    [SerializeField] private Button addDiamondsButton;
    [SerializeField] private bool enableDebugCurrencyButtons;

    protected override void OnEnable()
    {
        base.OnEnable();
        PlayerCurrencyStorage.OnCurrencyChanged += HandleCurrencyChanged;
        Refresh();
    }

    protected override void Start()
    {
        base.Start();
        BindButtons();
    }

    protected override void OnDisable()
    {
        PlayerCurrencyStorage.OnCurrencyChanged -= HandleCurrencyChanged;
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        UnbindButtons();
        base.OnDestroy();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadTexts();
        LoadButtons();
    }

    public void Refresh()
    {
        SetText(coinsText, PlayerCurrencyStorage.Coins.ToString("N0"));
        SetText(diamondsText, PlayerCurrencyStorage.Diamonds.ToString("N0"));
    }

    private void HandleCurrencyChanged(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Diamonds:
                SetText(diamondsText, amount.ToString("N0"));
                break;
            default:
                SetText(coinsText, amount.ToString("N0"));
                break;
        }
    }

    private void LoadTexts()
    {
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in tmpTexts)
        {
            string textName = text.name.ToLowerInvariant();

            if (coinsText == null && textName.Contains("coin"))
            {
                coinsText = text;
                continue;
            }

            if (diamondsText == null && (textName.Contains("diamond") || textName.Contains("gem")))
            {
                diamondsText = text;
            }
        }
    }

    private void LoadButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button foundButton in buttons)
        {
            if (foundButton == null) continue;
            if (foundButton.GetComponent<ButtonLobbySection>() != null) continue;

            if (addCoinsButton == null && IsUnderTransformName(foundButton.transform, "coin"))
            {
                addCoinsButton = foundButton;
                continue;
            }

            if (addDiamondsButton == null &&
                (IsUnderTransformName(foundButton.transform, "diamond") || IsUnderTransformName(foundButton.transform, "gem")))
            {
                addDiamondsButton = foundButton;
            }
        }
    }

    private void BindButtons()
    {
        ConfigureDebugCurrencyButton(addCoinsButton);
        ConfigureDebugCurrencyButton(addDiamondsButton);

        if (!enableDebugCurrencyButtons) return;

        if (addCoinsButton != null)
        {
            addCoinsButton.onClick.RemoveListener(AddTestCoins);
            addCoinsButton.onClick.AddListener(AddTestCoins);
        }

        if (addDiamondsButton != null)
        {
            addDiamondsButton.onClick.RemoveListener(AddTestDiamonds);
            addDiamondsButton.onClick.AddListener(AddTestDiamonds);
        }
    }

    private void UnbindButtons()
    {
        if (addCoinsButton != null)
            addCoinsButton.onClick.RemoveListener(AddTestCoins);

        if (addDiamondsButton != null)
            addDiamondsButton.onClick.RemoveListener(AddTestDiamonds);
    }

    private void AddTestCoins()
    {
        PlayerCurrencyStorage.Add(CurrencyType.Coins, TestCoinAmount);
    }

    private void AddTestDiamonds()
    {
        PlayerCurrencyStorage.Add(CurrencyType.Diamonds, TestDiamondAmount);
    }

    private void ConfigureDebugCurrencyButton(Button target)
    {
        if (target == null) return;

        target.gameObject.SetActive(enableDebugCurrencyButtons);
    }

    private bool IsUnderTransformName(Transform target, string value)
    {
        while (target != null && target != transform)
        {
            if (target.name.ToLowerInvariant().Contains(value))
                return true;

            target = target.parent;
        }

        return false;
    }

    private void SetText(TMP_Text tmpText, string value)
    {
        if (tmpText != null)
        {
            tmpText.text = value;
        }
    }
}
