using TMPro;
using UnityEngine;

/// <summary>Controls one stat row in the Forge Enhance attributes UI.</summary>
public sealed class ForgeEnhanceStatRow : MonoBehaviour
{
    [SerializeField] private StatType statType;
    [SerializeField] private TextMeshProUGUI presentText;
    [SerializeField] private GameObject arrow;
    [SerializeField] private TextMeshProUGUI afterText;

    public StatType StatType => statType;

    private void Reset()
    {
        LoadComponents();
        LoadStatTypeFromName();
    }

    private void OnValidate()
    {
        LoadComponents();
    }

    public void SetValues(float present, float after, bool visible)
    {
        gameObject.SetActive(visible);
        if (!visible)
            return;

        if (presentText != null)
            presentText.text = $"{GetDisplayName()}: {present:0.#}";

        if (afterText != null)
            afterText.text = after.ToString("0.#");

        if (arrow != null)
            arrow.SetActive(true);
    }

    private void LoadComponents()
    {
        if (presentText == null)
            presentText = FindText("Present");

        if (afterText == null)
            afterText = FindText("After");

        if (arrow == null)
            arrow = transform.Find("Arrow")?.gameObject;
    }

    private void LoadStatTypeFromName()
    {
        if (statType != StatType.None)
            return;

        string statName = gameObject.name;
        if (statName.Equals("Health", System.StringComparison.OrdinalIgnoreCase))
            statName = nameof(StatType.MaxHealth);

        if (System.Enum.TryParse(statName, true, out StatType parsed))
            statType = parsed;
    }

    private TextMeshProUGUI FindText(string objectName)
    {
        foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                return text;
        }

        return null;
    }

    private string GetDisplayName()
    {
        return statType switch
        {
            StatType.MaxHealth => "Health",
            StatType.Attack => "Attack",
            StatType.Armor => "Armor",
            StatType.MaxMana => "Mana",
            StatType.MoveSpeed => "Move Speed",
            StatType.AttackSpeed => "Attack Speed",
            StatType.CritChance => "Crit Chance",
            StatType.CritDamage => "Crit Damage",
            StatType.HealthRegen => "Health Regen",
            StatType.ManaRegen => "Mana Regen",
            _ => statType.ToString()
        };
    }
}
