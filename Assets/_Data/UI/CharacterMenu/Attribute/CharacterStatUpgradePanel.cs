using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatUpgradePanel : BaseMonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text availablePointsText;
    [SerializeField] private TMP_Text attackPointsText;
    [SerializeField] private TMP_Text maxHealthPointsText;
    [SerializeField] private TMP_Text armorPointsText;
    [SerializeField] private TMP_Text critChancePointsText;
    [SerializeField] private TMP_Text critDamagePointsText;

    [Header("Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button maxHealthButton;
    [SerializeField] private Button armorButton;
    [SerializeField] private Button critChanceButton;
    [SerializeField] private Button critDamageButton;
    [SerializeField] private Button resetButton;

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
        LoadTexts();
        LoadButtons();
    }

    public void Refresh()
    {
        PlayerAttributePointStorage.EnsureLevelRewarded(PlayerExperienceStorage.Level);

        SetText(availablePointsText, PlayerAttributePointStorage.AvailablePoints.ToString("N0"));
        SetStatText(attackPointsText, StatType.Attack);
        SetStatText(maxHealthPointsText, StatType.MaxHealth);
        SetStatText(armorPointsText, StatType.Armor);
        SetStatText(critChancePointsText, StatType.CritChance);
        SetStatText(critDamagePointsText, StatType.CritDamage);

        bool canSpend = PlayerAttributePointStorage.AvailablePoints > 0;
        SetInteractable(attackButton, canSpend);
        SetInteractable(maxHealthButton, canSpend);
        SetInteractable(armorButton, canSpend);
        SetInteractable(critChanceButton, canSpend);
        SetInteractable(critDamageButton, canSpend);
    }

    private void SpendAttack() => Spend(StatType.Attack);
    private void SpendMaxHealth() => Spend(StatType.MaxHealth);
    private void SpendArmor() => Spend(StatType.Armor);
    private void SpendCritChance() => Spend(StatType.CritChance);
    private void SpendCritDamage() => Spend(StatType.CritDamage);

    private void Spend(StatType statType)
    {
        PlayerAttributePointStorage.TrySpendPoint(statType);
    }

    private void ResetSpentPoints()
    {
        PlayerAttributePointStorage.ResetSpentPoints();
    }

    private void BindButtons()
    {
        BindButton(attackButton, SpendAttack);
        BindButton(maxHealthButton, SpendMaxHealth);
        BindButton(armorButton, SpendArmor);
        BindButton(critChanceButton, SpendCritChance);
        BindButton(critDamageButton, SpendCritDamage);
        BindButton(resetButton, ResetSpentPoints);
    }

    private void UnbindButtons()
    {
        UnbindButton(attackButton, SpendAttack);
        UnbindButton(maxHealthButton, SpendMaxHealth);
        UnbindButton(armorButton, SpendArmor);
        UnbindButton(critChanceButton, SpendCritChance);
        UnbindButton(critDamageButton, SpendCritDamage);
        UnbindButton(resetButton, ResetSpentPoints);
    }

    private void LoadTexts()
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null) continue;

            string textName = text.name.ToLowerInvariant();

            if (availablePointsText == null &&
                (textName.Contains("available") || textName.Contains("unspent") || textName.Contains("remaining") || textName.Contains("free")))
            {
                availablePointsText = text;
                continue;
            }

            if (attackPointsText == null && textName.Contains("attack"))
            {
                attackPointsText = text;
                continue;
            }

            if (maxHealthPointsText == null && (textName.Contains("health") || textName.Contains("hp")))
            {
                maxHealthPointsText = text;
                continue;
            }

            if (armorPointsText == null && textName.Contains("armor"))
            {
                armorPointsText = text;
                continue;
            }

            if (critChancePointsText == null && textName.Contains("crit") && textName.Contains("chance"))
            {
                critChancePointsText = text;
                continue;
            }

            if (critDamagePointsText == null && textName.Contains("crit") && textName.Contains("damage"))
                critDamagePointsText = text;
        }
    }

    private void LoadButtons()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            string buttonName = button.name.ToLowerInvariant();

            if (attackButton == null && buttonName.Contains("attack"))
            {
                attackButton = button;
                continue;
            }

            if (maxHealthButton == null && (buttonName.Contains("health") || buttonName.Contains("hp")))
            {
                maxHealthButton = button;
                continue;
            }

            if (armorButton == null && buttonName.Contains("armor"))
            {
                armorButton = button;
                continue;
            }

            if (critChanceButton == null && buttonName.Contains("crit") && buttonName.Contains("chance"))
            {
                critChanceButton = button;
                continue;
            }

            if (critDamageButton == null && buttonName.Contains("crit") && buttonName.Contains("damage"))
            {
                critDamageButton = button;
                continue;
            }

            if (resetButton == null && (buttonName.Contains("reset") || buttonName.Contains("refund")))
                resetButton = button;
        }
    }

    private void SetStatText(TMP_Text text, StatType statType)
    {
        if (text == null) return;

        int points = PlayerAttributePointStorage.GetSpentPoints(statType);
        float bonus = PlayerAttributePointStorage.GetBonusValue(statType);
        text.text = "+" + points + " (" + FormatBonus(statType, bonus) + ")";
    }

    private string FormatBonus(StatType statType, float bonus)
    {
        if (statType == StatType.CritChance || statType == StatType.CritDamage)
            return "+" + (bonus * 100f).ToString("0.#") + "%";

        return "+" + bonus.ToString(bonus % 1f == 0f ? "0" : "0.#");
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private void SetInteractable(Button button, bool value)
    {
        if (button != null)
            button.interactable = value;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveListener(action);
    }
}
