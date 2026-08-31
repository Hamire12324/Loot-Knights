using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Attaches the character-selection view scripts to the authored UI hierarchy.</summary>
public static class CharacterSelectionViewSetup
{
    [MenuItem("Loot Knights/UI/Setup Character Selection Views")]
    public static void Setup()
    {
        CharacterSelectionPanel panel = Object.FindAnyObjectByType<CharacterSelectionPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Character Selection", "Không tìm thấy CharacterSelectionPanel.", "OK");
            return;
        }

        Transform content = panel.transform.Find("Content");
        Transform template = content != null ? content.Find("CharacterSlotTemplate") : null;
        Transform backButton = panel.transform.Find("Btn_Back") ?? panel.transform.Find("BackButton");

        if (content == null || template == null || backButton == null)
        {
            EditorUtility.DisplayDialog(
                "Character Selection",
                "Cần có Content, CharacterSlotTemplate và Btn_Back/BackButton trước khi setup.",
                "OK");
            return;
        }

        AddIfMissing<CharacterSelectionListView>(content.gameObject);
        AddIfMissing<CharacterSelectionSlotView>(template.gameObject);
        AddIfMissing<CharacterSelectionBackButton>(backButton.gameObject);

        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Selection.activeGameObject = panel.gameObject;
    }

    private static void AddIfMissing<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.GetComponent<T>() != null) return;

        Undo.AddComponent<T>(gameObject);
    }
}
