using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterStatUpgradePanel : BaseMonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Transform pointsHeader;
    [SerializeField] private TMP_Text availablePointsText;

    [Header("Cards")]
    [SerializeField] private Transform statUpgradeGrid;
    [SerializeField] private List<CharacterStatUpgradeCard> cards = new();

    protected override void OnEnable()
    {
        base.OnEnable();
        PlayerAttributePointStorage.OnPointsChanged += Refresh;
        Refresh();
    }

    protected override void OnDisable()
    {
        PlayerAttributePointStorage.OnPointsChanged -= Refresh;
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadPointsHeader();
        LoadAvailablePointsText();
        LoadStatUpgradeGrid();
        LoadCards();
    }

    public void Refresh()
    {
        PlayerAttributePointStorage.EnsureLevelRewarded(PlayerExperienceStorage.Level);

        if (availablePointsText != null)
            availablePointsText.text = "Point: " + PlayerAttributePointStorage.AvailablePoints.ToString("N0");

        foreach (CharacterStatUpgradeCard card in cards)
            card?.Refresh();
    }

    private void LoadPointsHeader()
    {
        if (pointsHeader != null) return;

        pointsHeader = FindChildByName(transform, "PointsHeader");
    }

    private void LoadAvailablePointsText()
    {
        if (availablePointsText != null) return;

        Transform searchRoot = pointsHeader != null ? pointsHeader : transform;
        foreach (TMP_Text text in searchRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null) continue;

            string textName = text.name.ToLowerInvariant();
            if (textName.Contains("point") ||
                textName.Contains("available") ||
                textName.Contains("unspent") ||
                textName.Contains("remaining") ||
                textName.Contains("free"))
            {
                availablePointsText = text;
                return;
            }
        }
    }

    private void LoadStatUpgradeGrid()
    {
        if (statUpgradeGrid != null) return;

        statUpgradeGrid = FindChildByName(transform, "StatUpgradeGrid");
        if (statUpgradeGrid == null)
            statUpgradeGrid = transform;
    }

    private void LoadCards()
    {
        cards.Clear();
        if (statUpgradeGrid == null) return;

        foreach (Transform child in statUpgradeGrid.GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == statUpgradeGrid) continue;
            if (!child.name.ToLowerInvariant().Contains("statupgradecard")) continue;

            CharacterStatUpgradeCard card = child.GetComponent<CharacterStatUpgradeCard>();
            if (card == null)
                continue;

            StatType statType = CharacterStatUpgradeCard.ResolveStatType(child.name);
            card.Configure(statType);

            if (!cards.Contains(card))
                cards.Add(card);
        }
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == targetName)
                return child;
        }

        return null;
    }

    private void OnValidate()
    {
        cards ??= new List<CharacterStatUpgradeCard>();
    }
}
