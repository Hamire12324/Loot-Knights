using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot Knights/Equipment/Equipment Set")]
public class EquipmentSetDefinition : ScriptableObject
{
    [SerializeField] private string setId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private List<EquipmentSetBonus> bonuses = new();

    public string SetId => setId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<EquipmentSetBonus> Bonuses => bonuses;
    public bool IsValid => !string.IsNullOrWhiteSpace(setId);

    public void AddActiveBonuses(int equippedPieceCount, List<StatModifier> output)
    {
        if (output == null || bonuses == null) return;

        foreach (EquipmentSetBonus bonus in bonuses)
        {
            if (bonus != null && equippedPieceCount >= bonus.RequiredPieceCount)
                bonus.AddModifiersTo(output);
        }
    }

    private void OnValidate()
    {
        bonuses ??= new List<EquipmentSetBonus>();
        bonuses.RemoveAll(bonus => bonus == null);

        foreach (EquipmentSetBonus bonus in bonuses)
            bonus.Validate();

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;
    }
}
