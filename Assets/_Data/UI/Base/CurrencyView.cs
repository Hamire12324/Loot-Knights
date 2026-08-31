using System.Globalization;
using TMPro;
using UnityEngine;

public class CurrencyView : BaseMonoBehaviour
{
    private const string CoinsBarName = "coinsbar";
    private const string DiamondBarName = "diamondbar";

    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI diamondsText;
    private PlayerCurrencyManager currencyManager;

    protected override void OnEnable()
    {
        base.OnEnable();

        currencyManager = PlayerCurrencyManager.Service;
        if (currencyManager != null)
        {
            currencyManager.OnCurrencyChanged -= HandleCurrencyChanged;
            currencyManager.OnCurrencyChanged += HandleCurrencyChanged;
        }
        Refresh();
    }

    protected override void OnDisable()
    {
        if (currencyManager != null)
            currencyManager.OnCurrencyChanged -= HandleCurrencyChanged;

        currencyManager = null;
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        LoadTextReferences();
    }

    public void Refresh()
    {
        LoadComponents();
        if (currencyManager == null)
            return;

        SetText(coinsText, currencyManager.Coins);
        SetText(diamondsText, currencyManager.Diamonds);
    }

    private void HandleCurrencyChanged(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Diamonds:
                SetText(diamondsText, amount);
                break;
            default:
                SetText(coinsText, amount);
                break;
        }
    }

    private void LoadTextReferences()
    {
        if (coinsText != null && diamondsText != null) return;

        foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text == null) continue;

            if (coinsText == null && HasParentNamed(text.transform, CoinsBarName))
            {
                coinsText = text;
                continue;
            }

            if (diamondsText == null && HasParentNamed(text.transform, DiamondBarName))
                diamondsText = text;
        }
    }

    private bool HasParentNamed(Transform target, string namePart)
    {
        for (Transform parent = target.parent; parent != null && parent != transform; parent = parent.parent)
        {
            if (parent.name.ToLowerInvariant().Contains(namePart))
                return true;
        }

        return false;
    }

    private void SetText(TextMeshProUGUI text, int value)
    {
        if (text == null) return;

        text.text = Mathf.Max(0, value).ToString("N0", CultureInfo.InvariantCulture);
    }
}
