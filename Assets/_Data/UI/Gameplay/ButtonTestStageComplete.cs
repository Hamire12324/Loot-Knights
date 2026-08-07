using UnityEngine;

public class ButtonTestStageComplete : ButtonAbstract
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private bool editorOnly = true;

    protected override void Awake()
    {
        base.Awake();

        if (IsEditorOnlyBlocked())
            gameObject.SetActive(false);
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadStageManager();
    }

    protected override void OnClick()
    {
        if (IsEditorOnlyBlocked())
            return;

        if (stageManager == null)
            LoadStageManager();

        if (stageManager == null)
        {
            Debug.LogError(transform.name + ": Missing StageManager reference.", gameObject);
            return;
        }

        stageManager.CompleteStage();
    }

    private void LoadStageManager()
    {
        if (stageManager != null) return;

        stageManager = FindAnyObjectByType<StageManager>(FindObjectsInactive.Include);
    }

    private bool IsEditorOnlyBlocked()
    {
        return editorOnly && !Application.isEditor;
    }
}
