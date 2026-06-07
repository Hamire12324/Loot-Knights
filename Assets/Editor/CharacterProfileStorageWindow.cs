using UnityEditor;
using UnityEngine;

public class CharacterProfileStorageWindow : EditorWindow
{
    [MenuItem("Tools/Loot Knights/Character Save Viewer")]
    private static void Open()
    {
        CharacterProfileStorageWindow window = GetWindow<CharacterProfileStorageWindow>("Character Save");
        window.minSize = new Vector2(360f, 220f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Saved Character", EditorStyles.boldLabel);
        EditorGUILayout.Space(6f);

        if (CharacterProfileStorage.HasCharacter())
        {
            CreatedCharacterData character = CharacterProfileStorage.Load();

            EditorGUILayout.LabelField("Name", character.CharacterName);
            EditorGUILayout.LabelField("Class", character.CharacterClass.ToString());
        }
        else
        {
            EditorGUILayout.HelpBox("No character saved.", MessageType.Info);
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("PlayerPrefs Keys", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(CharacterProfileStorage.NameKey, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel(CharacterProfileStorage.ClassKey, GUILayout.Height(EditorGUIUtility.singleLineHeight));

        EditorGUILayout.Space(10f);

        using (new EditorGUI.DisabledScope(!CharacterProfileStorage.HasCharacter()))
        {
            if (GUILayout.Button("Delete Saved Character", GUILayout.Height(28f)))
            {
                if (EditorUtility.DisplayDialog("Delete Saved Character", "Delete the saved character from PlayerPrefs?", "Delete", "Cancel"))
                {
                    CharacterProfileStorage.Delete();
                    Repaint();
                }
            }
        }

        if (GUILayout.Button("Refresh", GUILayout.Height(24f)))
        {
            Repaint();
        }
    }
}
