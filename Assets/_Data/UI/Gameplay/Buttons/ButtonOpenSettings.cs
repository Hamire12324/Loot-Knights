using UnityEngine;

/// <summary>
/// Reusable button action that opens the persistent game settings screen.
/// Attach this to any UI Button that should show GameSettingsPanel.
/// </summary>
public class ButtonOpenSettings : ButtonAbstract
{
    protected override void OnClick()
    {
        SettingsPanel settingsPanel = SettingsPanel.Instance;
        if (settingsPanel == null)
        {
            settingsPanel = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        }

        if (settingsPanel == null)
        {
            Debug.LogWarning("ButtonOpenSettings: Missing GameSettingsPanel (SettingsPanel component).", gameObject);
            return;
        }

        settingsPanel.Show();
    }
}
