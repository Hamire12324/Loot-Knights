using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatUpgradeCard : BaseMonoBehaviour
{
    [SerializeField] private StatType statType = StatType.None;

    [Header("Texts")]
    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text bonusText;

    [Header("Stepper")]
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;

    public StatType StatType => statType;

    protected override void OnEnable()
    {
        base.OnEnable();
        PlayerAttributePointStorage.OnPointsChanged += Refresh;
        BindButtons();
        Refresh();
    }

    protected override void OnDisable()
    {
        PlayerAttributePointStorage.OnPointsChanged -= Refresh;
        UnbindButtons();
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        ResolveStatTypeFromObjectName();
        LoadTexts();
        LoadButtons();
    }

    public void Configure(StatType targetStatType)
    {
        if (targetStatType != StatType.None)
            statType = targetStatType;

        Refresh();
    }

    public void Refresh()
    {
        if (statType == StatType.None)
            ResolveStatTypeFromObjectName();

        int level = PlayerAttributePointStorage.GetSpentPoints(statType);
        float bonus = PlayerAttributePointStorage.GetBonusValue(statType);

        SetText(statNameText, GetDisplayName(statType));
        SetDescriptionText();
        SetText(levelText, level.ToString("N0"));
        SetText(bonusText, "Bonus: " + FormatBonus(statType, bonus));

        if (decreaseButton != null)
            decreaseButton.interactable = level > 0;

        if (increaseButton != null)
            increaseButton.interactable = PlayerAttributePointStorage.AvailablePoints > 0 &&
                                          PlayerAttributePointStorage.CanSpendOn(statType);
    }

    private void Increase()
    {
        PlayerAttributePointStorage.TrySpendPoint(statType);
    }

    private void Decrease()
    {
        PlayerAttributePointStorage.TryRefundPoint(statType);
    }

    private void BindButtons()
    {
        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveListener(Increase);
            increaseButton.onClick.AddListener(Increase);
        }

        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveListener(Decrease);
            decreaseButton.onClick.AddListener(Decrease);
        }
    }

    private void UnbindButtons()
    {
        if (increaseButton != null)
            increaseButton.onClick.RemoveListener(Increase);

        if (decreaseButton != null)
            decreaseButton.onClick.RemoveListener(Decrease);
    }

    private void ResolveStatTypeFromObjectName()
    {
        if (statType != StatType.None) return;

        statType = ResolveStatType(name);
    }

    public static StatType ResolveStatType(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return StatType.None;

        string lowerName = sourceName.ToLowerInvariant();

        if (lowerName.Contains("crit") && lowerName.Contains("chance"))
            return StatType.CritChance;

        if (lowerName.Contains("crit") && lowerName.Contains("damage"))
            return StatType.CritDamage;

        if (lowerName.Contains("attack"))
            return StatType.Attack;

        if (lowerName.Contains("maxhealth") || lowerName.Contains("health") || lowerName.Contains("hp"))
            return StatType.MaxHealth;

        if (lowerName.Contains("armor"))
            return StatType.Armor;

        return StatType.None;
    }

    private void LoadTexts()
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null) continue;

            string textName = text.name.ToLowerInvariant();

            if (statNameText == null && textName.Contains("statname"))
            {
                statNameText = text;
                continue;
            }

            if (descriptionText == null && textName.Contains("description"))
            {
                descriptionText = text;
                continue;
            }

            if (levelText == null && (textName.Contains("level") || textName.Contains("point")))
            {
                levelText = text;
                continue;
            }

            if (bonusText == null && textName.Contains("bonus"))
                bonusText = text;
        }
    }

    private void LoadButtons()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string buttonName = button.name.ToLowerInvariant();

            if (decreaseButton == null &&
                (buttonName.Contains("decrease") || buttonName.Contains("minus") || buttonName.Contains("remove")))
            {
                decreaseButton = button;
                continue;
            }

            if (increaseButton == null &&
                (buttonName.Contains("increase") || buttonName.Contains("plus") || buttonName.Contains("add") || buttonName.Contains("allocate")))
            {
                increaseButton = button;
            }
        }
    }

    private void SetDescriptionText()
    {
        if (descriptionText == null) return;
        if (!string.IsNullOrWhiteSpace(descriptionText.text)) return;

        descriptionText.text = GetDescription(statType);
    }

    private static string GetDisplayName(StatType targetStatType)
    {
        return targetStatType switch
        {
            StatType.Attack => "Attack",
            StatType.MaxHealth => "Max Health",
            StatType.Armor => "Armor",
            StatType.CritChance => "Crit Chance",
            StatType.CritDamage => "Crit Damage",
            _ => "Stat"
        };
    }

    private static string GetDescription(StatType targetStatType)
    {
        return targetStatType switch
        {
            StatType.Attack => "+2 ATK / level",
            StatType.MaxHealth => "+10 HP / level",
            StatType.Armor => "+0.5 ARM / level",
            StatType.CritChance => "+0.5% chance / level",
            StatType.CritDamage => "+2% damage / level",
            _ => string.Empty
        };
    }

    private static string FormatBonus(StatType targetStatType, float bonus)
    {
        if (targetStatType == StatType.CritChance || targetStatType == StatType.CritDamage)
            return "+" + (bonus * 100f).ToString("0.#") + "%";

        return "+" + bonus.ToString(bonus % 1f == 0f ? "0" : "0.#");
    }

    private static void SetText(TMP_Text targetText, string value)
    {
        if (targetText != null)
            targetText.text = value;
    }
}
