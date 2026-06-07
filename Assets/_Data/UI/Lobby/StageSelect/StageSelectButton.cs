using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectButton : ButtonAbstract
{
    [SerializeField] private StageSelectPanel stageSelectPanel;
    [SerializeField] private int stageIndex = -1;
    [SerializeField] private TMP_Text stageNumberText;
    [SerializeField] private GameObject lockedRoot;

    public bool HasStageIndex => stageIndex >= 0;
    public int StageIndex => Mathf.Max(0, stageIndex);

    public void SetStageSelectPanel(StageSelectPanel panel)
    {
        stageSelectPanel = panel;
    }

    public void SetStageIndex(int index)
    {
        stageIndex = Mathf.Max(0, index);
        RefreshDisplay();
    }

    public void SetAvailable(bool available)
    {
        gameObject.SetActive(available);
    }

    public void SetUnlocked(bool unlocked)
    {
        if (button != null)
            button.interactable = unlocked;

        if (lockedRoot != null)
            lockedRoot.SetActive(!unlocked);
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadStageSelectPanel();
        LoadStageNumberText();
        LoadLockedRoot();
    }

    protected override void OnClick()
    {
        if (stageSelectPanel == null)
            LoadStageSelectPanel();

        if (stageSelectPanel == null)
        {
            Debug.LogError(transform.name + ": Missing StageSelectPanel reference.", gameObject);
            return;
        }

        stageSelectPanel.SelectStage(StageIndex);
    }

    private void RefreshDisplay()
    {
        if (stageNumberText == null)
            LoadStageNumberText();

        if (stageNumberText != null)
            stageNumberText.text = (StageIndex + 1).ToString();
    }

    private void LoadStageSelectPanel()
    {
        if (stageSelectPanel != null) return;

        stageSelectPanel = GetComponentInParent<StageSelectPanel>();

        if (stageSelectPanel == null)
        {
            stageSelectPanel = FindAnyObjectByType<StageSelectPanel>(FindObjectsInactive.Include);
        }
    }

    private void LoadStageNumberText()
    {
        if (stageNumberText != null) return;

        stageNumberText = GetComponentInChildren<TMP_Text>(true);
    }

    private void LoadLockedRoot()
    {
        if (lockedRoot == null)
            lockedRoot = FindChildByName("locked", "lock");
    }

    private GameObject FindChildByName(params string[] keywords)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        if (children == null || keywords == null) return null;

        foreach (Transform child in children)
        {
            if (child == null || child == transform) continue;

            string childName = child.name.ToLowerInvariant();

            foreach (string keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;
                if (childName.Contains(keyword))
                    return child.gameObject;
            }
        }

        return null;
    }
}
