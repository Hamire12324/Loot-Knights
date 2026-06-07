using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterPreviewCaptureWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/_Data/UI/CharacterPreview";

    private enum CaptureMode
    {
        FullBody,
        Portrait
    }

    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private CaptureMode captureMode = CaptureMode.FullBody;
    [SerializeField] private string outputFolder = DefaultOutputFolder;
    [SerializeField] private string fileName = "CharacterPreview";
    [SerializeField] private int textureSize = 1024;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private bool autoFrame = true;
    [SerializeField] private float framePadding = 1.15f;
    [SerializeField] private float portraitVerticalPosition = 0.72f;
    [SerializeField] private float portraitSizeMultiplier = 0.45f;
    [SerializeField] private float orthographicSize = 2f;
    [SerializeField] private Vector3 prefabRotation;
    [SerializeField] private Vector3 prefabScale = Vector3.one;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

    [MenuItem("Tools/Loot Knights/Character Preview Capture")]
    private static void Open()
    {
        CharacterPreviewCaptureWindow window = GetWindow<CharacterPreviewCaptureWindow>("Character Preview");
        window.minSize = new Vector2(380f, 430f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Character Source", EditorStyles.boldLabel);
        characterPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", characterPrefab, typeof(GameObject), false);
        captureMode = (CaptureMode)EditorGUILayout.EnumPopup("Capture Mode", captureMode);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Folder", outputFolder);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        textureSize = EditorGUILayout.IntPopup("Texture Size", textureSize, new[] { "256", "512", "1024", "2048" }, new[] { 256, 512, 1024, 2048 });

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
        backgroundColor = EditorGUILayout.ColorField("Background", backgroundColor);
        autoFrame = EditorGUILayout.Toggle("Auto Frame", autoFrame);
        framePadding = EditorGUILayout.Slider("Frame Padding", framePadding, 1f, 2f);

        if (captureMode == CaptureMode.Portrait)
        {
            portraitVerticalPosition = EditorGUILayout.Slider("Portrait Height", portraitVerticalPosition, 0.45f, 0.95f);
            portraitSizeMultiplier = EditorGUILayout.Slider("Portrait Size", portraitSizeMultiplier, 0.2f, 0.8f);
        }

        using (new EditorGUI.DisabledScope(autoFrame))
        {
            orthographicSize = EditorGUILayout.FloatField("Orthographic Size", orthographicSize);
        }

        cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", cameraOffset);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Prefab Transform", EditorStyles.boldLabel);
        prefabRotation = EditorGUILayout.Vector3Field("Rotation", prefabRotation);
        prefabScale = EditorGUILayout.Vector3Field("Scale", prefabScale);

        EditorGUILayout.Space(14f);

        using (new EditorGUI.DisabledScope(characterPrefab == null))
        {
            if (GUILayout.Button("Capture PNG", GUILayout.Height(34f)))
            {
                Capture();
            }
        }
    }

    private void Capture()
    {
        if (characterPrefab == null)
        {
            EditorUtility.DisplayDialog("Character Preview Capture", "Choose a character prefab first.", "OK");
            return;
        }

        string sanitizedFileName = SanitizeFileName(fileName);

        if (string.IsNullOrEmpty(sanitizedFileName))
        {
            sanitizedFileName = characterPrefab.name;
        }

        EnsureOutputFolder();

        Scene previewScene = EditorSceneManager.NewPreviewScene();
        Camera previewCamera = null;
        RenderTexture renderTexture = null;
        Texture2D outputTexture = null;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            GameObject instance = InstantiatePreviewPrefab(previewScene);
            Bounds bounds = CalculateBounds(instance);
            Vector3 cameraTarget = CalculateCameraTarget(bounds);

            GameObject cameraObject = new GameObject("Character Preview Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);

            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = backgroundColor;
            previewCamera.orthographic = true;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 100f;
            previewCamera.transform.position = cameraTarget + cameraOffset;
            previewCamera.transform.rotation = Quaternion.identity;
            previewCamera.orthographicSize = autoFrame ? CalculateOrthographicSize(bounds, captureMode) : Mathf.Max(0.01f, orthographicSize);

            renderTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 1;
            previewCamera.targetTexture = renderTexture;

            previewCamera.Render();
            RenderTexture.active = renderTexture;

            outputTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            outputTexture.ReadPixels(new Rect(0f, 0f, textureSize, textureSize), 0, 0);
            outputTexture.Apply();

            RenderTexture.active = previousActive;

            string assetPath = outputFolder.TrimEnd('/', '\\') + "/" + sanitizedFileName + ".png";
            File.WriteAllBytes(assetPath, outputTexture.EncodeToPNG());

            AssetDatabase.ImportAsset(assetPath);
            ConfigureSpriteImporter(assetPath);
            AssetDatabase.Refresh();

            Object capturedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Selection.activeObject = capturedSprite != null ? capturedSprite : AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            EditorUtility.DisplayDialog("Character Preview Capture", "Saved preview:\n" + assetPath, "OK");
        }
        finally
        {
            RenderTexture.active = previousActive;

            if (previewCamera != null)
            {
                previewCamera.targetTexture = null;
            }

            if (outputTexture != null)
            {
                DestroyImmediate(outputTexture);
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }

            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private GameObject InstantiatePreviewPrefab(Scene previewScene)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(characterPrefab, previewScene) as GameObject;

        if (instance == null)
        {
            instance = Instantiate(characterPrefab);
            SceneManager.MoveGameObjectToScene(instance, previewScene);
        }

        instance.name = characterPrefab.name + "_PreviewInstance";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.Euler(prefabRotation);
        instance.transform.localScale = prefabScale;

        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            animator.Update(0f);
        }

        return instance;
    }

    private Bounds CalculateBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(instance.transform.position, Vector3.one);

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private Vector3 CalculateCameraTarget(Bounds bounds)
    {
        if (captureMode == CaptureMode.FullBody)
        {
            return bounds.center;
        }

        float portraitY = Mathf.Lerp(bounds.min.y, bounds.max.y, portraitVerticalPosition);
        return new Vector3(bounds.center.x, portraitY, bounds.center.z);
    }

    private float CalculateOrthographicSize(Bounds bounds, CaptureMode mode)
    {
        float heightSize = bounds.extents.y;
        float widthSize = bounds.extents.x;
        float size = Mathf.Max(heightSize, widthSize);

        if (mode == CaptureMode.Portrait)
        {
            size *= portraitSizeMultiplier;
        }

        return Mathf.Max(0.01f, size * framePadding);
    }

    private void EnsureOutputFolder()
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            outputFolder = DefaultOutputFolder;
        }

        string projectPath = Directory.GetCurrentDirectory().Replace("\\", "/");
        string absolutePath = Path.Combine(projectPath, outputFolder).Replace("\\", "/");

        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
        }
    }

    private void ConfigureSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private string SanitizeFileName(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return string.Empty;

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            source = source.Replace(invalidChar.ToString(), string.Empty);
        }

        return source.Trim();
    }
}
