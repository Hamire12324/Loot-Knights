using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageSelectGridView
{
    private readonly Transform root;
    private readonly StageSelectButton prefab;
    private readonly List<StageSelectButton> buttons;

    public int ButtonCount => buttons.Count;

    public StageSelectGridView(Transform root, StageSelectButton prefab, StageSelectButton[] serializedButtons)
    {
        this.root = root;
        this.prefab = prefab;
        buttons = serializedButtons != null && serializedButtons.Length > 0
            ? new List<StageSelectButton>(serializedButtons)
            : new List<StageSelectButton>(root.GetComponentsInChildren<StageSelectButton>(true));
    }

    public void Initialize(StageSelectPanel panel, int requiredButtonCount)
    {
        StageSelectButton sourcePrefab = prefab != null ? prefab : buttons.Count > 0 ? buttons[0] : null;
        while (sourcePrefab != null && buttons.Count < requiredButtonCount)
        {
            StageSelectButton button = UnityEngine.Object.Instantiate(sourcePrefab, root);
            button.name = $"StageButton_{buttons.Count + 1:D2}";
            buttons.Add(button);
        }

        foreach (StageSelectButton button in buttons)
        {
            button?.SetStageSelectPanel(panel);
        }
    }

    public void ShowPage(int pageIndex, int pageSize, int stageCount, Func<int, bool> isUnlocked)
    {
        int firstStageIndex = pageIndex * pageSize;

        for (int buttonIndex = 0; buttonIndex < buttons.Count; buttonIndex++)
        {
            StageSelectButton button = buttons[buttonIndex];
            if (button == null) continue;

            int stageIndex = firstStageIndex + buttonIndex;
            bool available = buttonIndex < pageSize && stageIndex < stageCount;
            button.SetAvailable(available);
            if (!available) continue;

            button.SetStageIndex(stageIndex);
            button.SetUnlocked(isUnlocked(stageIndex));
        }
    }
}
