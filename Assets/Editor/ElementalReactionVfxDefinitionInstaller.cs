using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ElementalReactionVfxDefinitionInstaller
{
    private const string DefinitionFolder = "Assets/Resources/VFX/Elemental/Reactions";
    private const string EffectPath = "Assets/Resources/Element/ElementalConduitEffect.asset";

    private readonly struct ReactionVfxBinding
    {
        public ReactionVfxBinding(ElementalReactionType reaction, string prefabPath)
        {
            Reaction = reaction;
            PrefabPath = prefabPath;
        }

        public ElementalReactionType Reaction { get; }
        public string PrefabPath { get; }
    }

    private static readonly ReactionVfxBinding[] Bindings =
    {
        new(ElementalReactionType.Shatter,
            "Assets/_ThirdParty/Vefects/Flipbook VFX URP/Elements 2D/Shrapnel/VFX_2D_Shrapnel_03.prefab"),
        new(ElementalReactionType.Overload,
            "Assets/_ThirdParty/Vefects/Flipbook VFX URP/Elements 2D/Explosion/VFX_2D_ExplosionFire_01.prefab"),
        new(ElementalReactionType.Superconduct,
            "Assets/_ThirdParty/Vefects/Flipbook VFX URP/Elements 2D/Lightning/VFX_2D_Lightning_04.prefab"),
        new(ElementalReactionType.Burnout,
            "Assets/_ThirdParty/Vefects/Flipbook VFX URP/Elements 2D/Explosion/VFX_2D_ExplosionPoison_01.prefab"),
        new(ElementalReactionType.Neuroshock,
            "Assets/_ThirdParty/Vefects/Flipbook VFX URP/Elements 2D/Lightning/VFX_2D_Lightning_08.prefab"),
        new(ElementalReactionType.BrittleToxin,
            "Assets/_ThirdParty/Vefects/Flipbook VFX URP/Elements 2D/Poison/VFX_2D_Poison_Puff_02.prefab")
    };

    [MenuItem("Loot Knights/Elemental Conduit/Create Reaction VFX Definitions")]
    public static void CreateReactionVfxDefinitions()
    {
        EnsureFolder(DefinitionFolder);

        VFXDefinition[] definitions = new VFXDefinition[Bindings.Length];
        for (int i = 0; i < Bindings.Length; i++)
        {
            definitions[i] = CreateOrUpdateDefinition(Bindings[i]);
        }

        AssignDefinitionsToEffect(definitions);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Elemental reaction VFX definitions created and assigned.");
    }

    private static VFXDefinition CreateOrUpdateDefinition(ReactionVfxBinding binding)
    {
        PoolObj prefabPoolObj = EnsurePrefabPoolObj(binding.PrefabPath);
        if (prefabPoolObj == null)
        {
            Debug.LogWarning($"Missing or invalid VFX prefab: {binding.PrefabPath}");
            return null;
        }

        string assetPath = $"{DefinitionFolder}/{binding.Reaction}_VFXDefinition.asset";
        VFXDefinition definition = AssetDatabase.LoadAssetAtPath<VFXDefinition>(assetPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<VFXDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        definition.Prefab = prefabPoolObj;
        definition.Offset = new Vector3(0f, 0.15f, 0f);
        definition.MirrorHorizontallyByDirection = false;
        definition.FlipX = false;
        definition.FlipY = false;
        definition.ParentToAnchor = false;
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static PoolObj EnsurePrefabPoolObj(string prefabPath)
    {
        if (!File.Exists(prefabPath))
            return null;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return null;

        PoolObj existingPoolObj = prefab.GetComponent<PoolObj>();
        if (existingPoolObj != null)
            return existingPoolObj;

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        VFXPoolObj poolObj = root.GetComponent<VFXPoolObj>();
        if (poolObj == null)
            poolObj = root.AddComponent<VFXPoolObj>();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return prefab != null ? prefab.GetComponent<PoolObj>() : null;
    }

    private static void AssignDefinitionsToEffect(VFXDefinition[] definitions)
    {
        HeroSkillElementalConduitEffect effect =
            AssetDatabase.LoadAssetAtPath<HeroSkillElementalConduitEffect>(EffectPath);
        if (effect == null)
        {
            Debug.LogWarning($"Cannot find {EffectPath}. VFX definitions were created but not assigned.");
            return;
        }

        SerializedObject serializedEffect = new(effect);
        SerializedProperty overrides = serializedEffect.FindProperty("reactionVfxOverrides");
        if (overrides == null)
        {
            Debug.LogWarning("reactionVfxOverrides was not found on ElementalConduitEffect.", effect);
            return;
        }

        overrides.arraySize = Bindings.Length;
        for (int i = 0; i < Bindings.Length; i++)
        {
            SerializedProperty entry = overrides.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("reaction").enumValueIndex = (int)Bindings[i].Reaction;
            entry.FindPropertyRelative("impactVfx").objectReferenceValue =
                i < definitions.Length ? definitions[i] : null;
            entry.FindPropertyRelative("impactSfx").objectReferenceValue = null;
        }

        serializedEffect.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(effect);
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}

public static class ElementalConduitGameplayDebugInstaller
{
    private readonly struct ReactionDamageColliderBinding
    {
        public ReactionDamageColliderBinding(string prefabPath, float radius)
        {
            PrefabPath = prefabPath;
            Radius = radius;
        }

        public string PrefabPath { get; }
        public float Radius { get; }
    }

    private static readonly ReactionDamageColliderBinding[] DamageColliderBindings =
    {
        new("Assets/_Data/VFX/Prefabs/Element/VFX_Shatter.prefab", 1.25f),
        new("Assets/_Data/VFX/Prefabs/Element/VFX_Overload.prefab", 1.45f),
        new("Assets/_Data/VFX/Prefabs/Element/VFX_Superconduct.prefab", 1.2f),
        new("Assets/_Data/VFX/Prefabs/Element/VFX_Burnout.prefab", 1.35f),
        new("Assets/_Data/VFX/Prefabs/Element/VFX_Neuroshock.prefab", 1.2f),
        new("Assets/_Data/VFX/Prefabs/Element/VFX_BrittleToxin.prefab", 1.25f)
    };

    private readonly struct ElementShardOrbBinding
    {
        public ElementShardOrbBinding(
            ElementType element,
            string name,
            string prefabPath,
            string sourcePrefabPath,
            string definitionPath,
            Color brightColor,
            Color darkColor)
        {
            Element = element;
            Name = name;
            PrefabPath = prefabPath;
            SourcePrefabPath = sourcePrefabPath;
            DefinitionPath = definitionPath;
            BrightColor = brightColor;
            DarkColor = darkColor;
        }

        public ElementType Element { get; }
        public string Name { get; }
        public string PrefabPath { get; }
        public string SourcePrefabPath { get; }
        public string DefinitionPath { get; }
        public Color BrightColor { get; }
        public Color DarkColor { get; }
    }

    private const string ElementOrbMaterialFolder = "Assets/_Data/VFX/Materials/Element/Orb";
    private const string ElementIconSetPath = "Assets/Resources/Element/ElementalIconSet.asset";

    private static readonly ElementShardOrbBinding[] ElementOrbBindings =
    {
        new(
            ElementType.Fire,
            "Fire",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Fire_Orb.prefab",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Fire_Orb.prefab",
            "Assets/Resources/VFX/Elemental/Element/Fire.asset",
            new Color(1.65f, 0.48f, 0.08f, 1f),
            new Color(0.55f, 0.04f, 0.01f, 1f)),
        new(
            ElementType.Frost,
            "Frost",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Frost_Orb.prefab",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Frost_Orb.prefab",
            "Assets/Resources/VFX/Elemental/Element/Frost.asset",
            new Color(0.32f, 1.35f, 1.85f, 1f),
            new Color(0.02f, 0.2f, 0.48f, 1f)),
        new(
            ElementType.Lightning,
            "Lightning",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Lightning_Orb.prefab",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Frost_Orb.prefab",
            "Assets/Resources/VFX/Elemental/Element/Lightning.asset",
            new Color(1.7f, 1.45f, 0.12f, 1f),
            new Color(0.26f, 0.04f, 0.7f, 1f)),
        new(
            ElementType.Poison,
            "Poison",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Poison_Orb.prefab",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Poision_Orb.prefab",
            "Assets/Resources/VFX/Elemental/Element/Poison.asset",
            new Color(0.42f, 1.65f, 0.1f, 1f),
            new Color(0.02f, 0.35f, 0.04f, 1f))
    };

    [MenuItem("Loot Knights/Elemental Conduit/Add Gameplay Add-All-Elements Button")]
    public static void AddGameplayAddAllElementsButton()
    {
        GameObject existing = GameObject.Find("Btn_AddAllElements");
        if (existing != null)
        {
            EnsureButtonComponents(existing);
            Selection.activeGameObject = existing;
            Debug.Log("Btn_AddAllElements already exists. Selected existing button.");
            return;
        }

        GameObject parent = GameObject.Find("GamePlayPanel") ?? GameObject.Find("UI");
        if (parent == null)
        {
            Debug.LogError("Cannot create Btn_AddAllElements. Missing GamePlayPanel or UI in the open scene.");
            return;
        }

        GameObject buttonObject = new("Btn_AddAllElements", typeof(RectTransform), typeof(CanvasRenderer));
        Undo.RegisterCreatedObjectUndo(buttonObject, "Create Add All Elements Button");
        buttonObject.layer = parent.layer;
        buttonObject.transform.SetParent(parent.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(270f, -165f);
        rect.sizeDelta = new Vector2(96f, 42f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.07f, 0.16f, 0.28f, 0.78f);
        image.raycastTarget = true;

        EnsureButtonComponents(buttonObject);
        AddLabel(buttonObject.transform);

        EditorSceneManager.MarkSceneDirty(buttonObject.scene);
        Selection.activeGameObject = buttonObject;
        Debug.Log("Created Btn_AddAllElements. Move/style it freely; runtime code will not reposition it.");
    }

    [MenuItem("Loot Knights/Elemental Conduit/Add Damage Colliders To Reaction VFX")]
    public static void AddDamageCollidersToReactionVfx()
    {
        int updatedCount = 0;
        foreach (ReactionDamageColliderBinding binding in DamageColliderBindings)
        {
            if (!File.Exists(binding.PrefabPath))
            {
                Debug.LogWarning($"Cannot add damage collider. Missing prefab: {binding.PrefabPath}");
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(binding.PrefabPath);
            CircleCollider2D collider = root.GetComponentInChildren<CircleCollider2D>(true);
            if (collider == null)
                collider = root.AddComponent<CircleCollider2D>();

            collider.isTrigger = true;
            collider.enabled = true;
            collider.offset = Vector2.zero;
            collider.radius = Mathf.Max(0.05f, binding.Radius);

            PrefabUtility.SaveAsPrefabAsset(root, binding.PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            updatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Updated {updatedCount} elemental reaction VFX prefabs with trigger CircleCollider2D damage colliders.");
    }

    [MenuItem("Loot Knights/Elemental Conduit/Setup 4 Element Shard Orb VFX")]
    public static void SetupFourElementShardOrbVfx()
    {
        EnsureFolder(ElementOrbMaterialFolder);
        EnsureFolder("Assets/Resources/VFX/Elemental/Element");

        MoveAssetIfNeeded(
            "Assets/Resources/VFX/Elemental/Element/Forst.asset",
            "Assets/Resources/VFX/Elemental/Element/Frost.asset");
        MoveAssetIfNeeded(
            "Assets/Resources/VFX/Elemental/Element/Poision.asset",
            "Assets/Resources/VFX/Elemental/Element/Poison.asset");
        MoveAssetIfNeeded(
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Poision_Orb.prefab",
            "Assets/_Data/VFX/Prefabs/Element/VFX_2D_Poison_Orb.prefab");

        foreach (ElementShardOrbBinding binding in ElementOrbBindings)
        {
            EnsurePrefabExists(binding);
            SetupElementOrbPrefab(binding);
        }

        AssignElementOrbDefinitionsToIconSet();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Set up 4 independent element shard orb VFX: Fire, Frost, Lightning, Poison.");
    }

    private static void EnsurePrefabExists(ElementShardOrbBinding binding)
    {
        if (File.Exists(binding.PrefabPath))
            return;

        if (!File.Exists(binding.SourcePrefabPath))
        {
            Debug.LogWarning($"Cannot create {binding.Name} orb. Missing source prefab: {binding.SourcePrefabPath}");
            return;
        }

        bool copied = AssetDatabase.CopyAsset(binding.SourcePrefabPath, binding.PrefabPath);
        string error = copied ? string.Empty : "AssetDatabase.CopyAsset returned false.";

        if (!string.IsNullOrEmpty(error))
            Debug.LogWarning($"Could not copy {binding.SourcePrefabPath} to {binding.PrefabPath}: {error}");
    }

    private static void SetupElementOrbPrefab(ElementShardOrbBinding binding)
    {
        if (!File.Exists(binding.PrefabPath))
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(binding.PrefabPath);
        root.name = $"VFX_2D_{binding.Name}_Orb";

        VFXPoolObj poolObj = root.GetComponent<VFXPoolObj>();
        if (poolObj == null)
            poolObj = root.AddComponent<VFXPoolObj>();

        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startColor = new ParticleSystem.MinMaxGradient(
                GetElementOrbParticleColor(particleSystem.gameObject.name, binding.BrightColor));
        }

        ParticleSystemRenderer[] renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (ParticleSystemRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            renderer.sortingOrder = GetElementOrbSortingOrder(renderer.gameObject.name);
            renderer.sortingFudge = 0f;

            Material material = CreateElementMaterialCopy(binding, renderer);
            if (material != null)
                renderer.sharedMaterial = material;
        }

        PrefabUtility.SaveAsPrefabAsset(root, binding.PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(binding.PrefabPath);
        PoolObj prefabPoolObj = prefab != null ? prefab.GetComponent<PoolObj>() : null;
        CreateOrUpdateElementVfxDefinition(binding, prefabPoolObj);
    }

    private static Color GetElementOrbParticleColor(string particleSystemName, Color brightColor)
    {
        if (string.IsNullOrEmpty(particleSystemName))
            return brightColor;

        string normalizedName = particleSystemName.ToLowerInvariant();
        if (normalizedName.Contains("insidedrops"))
            return WithAlpha(brightColor, 0.32f);

        if (normalizedName.Contains("bgdrops") || normalizedName.Contains("darkglow"))
            return WithAlpha(brightColor, 0.55f);

        return brightColor;
    }

    private static int GetElementOrbSortingOrder(string rendererName)
    {
        if (string.IsNullOrEmpty(rendererName))
            return 0;

        string normalizedName = rendererName.ToLowerInvariant();
        if (normalizedName.Contains("darkglow"))
            return -40;

        if (normalizedName.Contains("bgdrops"))
            return -30;

        if (normalizedName.Contains("dark_insidedrops"))
            return -20;

        if (normalizedName.Contains("insidedrops"))
            return -10;

        if (normalizedName.Contains("inside_sphere"))
            return 0;

        if (normalizedName.Contains("otsidestrokes_back") || normalizedName.Contains("outsidestrokes_back"))
            return 10;

        if (normalizedName.Contains("otsidestrokes_front") || normalizedName.Contains("outsidestrokes_front"))
            return 20;

        return 0;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Material CreateElementMaterialCopy(
        ElementShardOrbBinding binding,
        ParticleSystemRenderer renderer)
    {
        Material source = renderer.sharedMaterial;
        string rendererName = SanitizeAssetName(renderer.gameObject.name);
        string materialPath = $"{ElementOrbMaterialFolder}/{binding.Name}_{rendererName}.mat";

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else if (material != source)
        {
            material.CopyPropertiesFromMaterial(source);
        }

        TintMaterial(material, binding.BrightColor, binding.DarkColor);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void TintMaterial(Material material, Color brightColor, Color darkColor)
    {
        Color midColor = Color.Lerp(darkColor, brightColor, 0.55f);
        Color outlineColor = Color.Lerp(darkColor, Color.black, 0.35f);

        SetMaterialColor(material, "_R", brightColor);
        SetMaterialColor(material, "_G", midColor);
        SetMaterialColor(material, "_B", darkColor);
        SetMaterialColor(material, "_Outline", outlineColor);
        SetMaterialColor(material, "_Color01", brightColor);
        SetMaterialColor(material, "_Color02", midColor);
        SetMaterialColor(material, "_Color03", darkColor);
        SetMaterialColor(material, "_Color04", outlineColor);
        SetMaterialColor(material, "_EmissionColor", brightColor);
    }

    private static void SetMaterialColor(Material material, string propertyName, Color color)
    {
        if (material != null && material.HasProperty(propertyName))
            material.SetColor(propertyName, color);
    }

    private static void CreateOrUpdateElementVfxDefinition(
        ElementShardOrbBinding binding,
        PoolObj prefabPoolObj)
    {
        if (prefabPoolObj == null)
        {
            Debug.LogWarning($"Cannot assign {binding.Name} shard VFX. Prefab has no PoolObj: {binding.PrefabPath}");
            return;
        }

        VFXDefinition definition = AssetDatabase.LoadAssetAtPath<VFXDefinition>(binding.DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<VFXDefinition>();
            AssetDatabase.CreateAsset(definition, binding.DefinitionPath);
        }

        definition.name = binding.Name;
        definition.Prefab = prefabPoolObj;
        definition.Offset = Vector3.zero;
        if (definition.Scale <= 0f)
            definition.Scale = 1f;
        definition.MirrorHorizontallyByDirection = false;
        definition.FlipX = false;
        definition.FlipY = false;
        definition.ParentToAnchor = true;
        EditorUtility.SetDirty(definition);
    }

    private static void AssignElementOrbDefinitionsToIconSet()
    {
        ElementalIconSet iconSet = AssetDatabase.LoadAssetAtPath<ElementalIconSet>(ElementIconSetPath);
        if (iconSet == null)
        {
            Debug.LogWarning($"Cannot assign shard VFX. Missing {ElementIconSetPath}");
            return;
        }

        SerializedObject serializedIconSet = new(iconSet);
        SerializedProperty elementIcons = serializedIconSet.FindProperty("elementIcons");
        if (elementIcons == null)
            return;

        for (int i = 0; i < elementIcons.arraySize; i++)
        {
            SerializedProperty entry = elementIcons.GetArrayElementAtIndex(i);
            SerializedProperty elementProperty = entry.FindPropertyRelative("Element");
            SerializedProperty shardVfxProperty = entry.FindPropertyRelative("ShardVfx");
            if (elementProperty == null || shardVfxProperty == null)
                continue;

            foreach (ElementShardOrbBinding binding in ElementOrbBindings)
            {
                if (elementProperty.enumValueIndex != (int)binding.Element)
                    continue;

                shardVfxProperty.objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<VFXDefinition>(binding.DefinitionPath);
                break;
            }
        }

        serializedIconSet.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(iconSet);
    }

    private static void MoveAssetIfNeeded(string oldPath, string newPath)
    {
        if (!File.Exists(oldPath) || File.Exists(newPath))
            return;

        string error = AssetDatabase.MoveAsset(oldPath, newPath);
        if (!string.IsNullOrEmpty(error))
            Debug.LogWarning($"Could not move {oldPath} to {newPath}: {error}");
    }

    private static string SanitizeAssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Renderer";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(' ', '_');
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static void EnsureButtonComponents(GameObject buttonObject)
    {
        if (buttonObject.GetComponent<Button>() == null)
            buttonObject.AddComponent<Button>();

        if (buttonObject.GetComponent<ButtonAddAllElements>() == null)
            buttonObject.AddComponent<ButtonAddAllElements>();
    }

    private static void AddLabel(Transform parent)
    {
        if (parent.Find("Label") != null)
            return;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObject.layer = parent.gameObject.layer;
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "ALL";
        label.fontSize = 20f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.95f, 0.58f, 1f);
        label.raycastTarget = false;
    }
}
