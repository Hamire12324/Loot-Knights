using UnityEngine;

public class ButtonQuitGame : ButtonAbstract
{
    [SerializeField] private MainMenuPanel mainMenuPanel;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (mainMenuPanel == null)
        {
            mainMenuPanel = GetComponentInParent<MainMenuPanel>();
        }
    }

    protected override void OnClick()
    {
        if (mainMenuPanel == null)
        {
            Debug.LogError(transform.name + ": Missing MainMenuPanel reference.", gameObject);
            return;
        }

        mainMenuPanel.RequestQuitGame();
    }
}
