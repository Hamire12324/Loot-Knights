using System.Collections.Generic;
using UnityEngine;

public class CharacterStatUpgradeGrid : BaseMonoBehaviour
{
    [SerializeField] private List<CharacterStatUpgradeCard> cards = new();

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadCards();
    }

    public void Refresh()
    {
        foreach (CharacterStatUpgradeCard card in cards)
            card?.Refresh();
    }

    private void LoadCards()
    {
        cards.Clear();

        foreach (CharacterStatUpgradeCard card in GetComponentsInChildren<CharacterStatUpgradeCard>(true))
        {
            if (!card.name.ToLowerInvariant().Contains("statupgradecard")) continue;

            card.Configure(CharacterStatUpgradeCard.ResolveStatType(card.name));
            cards.Add(card);
        }
    }

    private void OnValidate()
    {
        cards ??= new List<CharacterStatUpgradeCard>();
    }
}
