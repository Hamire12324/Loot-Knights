using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillViewHierarchyBuilder : EditorWindow
{
    private const string WindowTitle = "Skill View Builder";
    private const string DefaultsKey = "LootKnights.SkillViewBuilder.Defaults";
    private static readonly string[] LegacyDefaultsKeys =
    {
        "LootKnights.SkillViewBuilder.Defaults.v5",
        "LootKnights.SkillViewBuilder.Defaults.v4",
        "LootKnights.SkillViewBuilder.Defaults.v3"
    };
    private static readonly string[] GeneratedRootNames =
    {
        "SkillTreeRoot",
        "TreeArea",
        "DetailPanel",
        "EquipSkillPanel"
    };

    private readonly struct BranchDepthKey
    {
        public readonly SkillTreeBranch Branch;
        public readonly int Depth;

        public BranchDepthKey(SkillTreeBranch branch, int depth)
        {
            Branch = branch;
            Depth = depth;
        }
    }

    [SerializeField] private GameObject skillViewRoot;
    [SerializeField] private SkillTreeDefinition skillTree;
    [SerializeField] private SkillTreeDefinition secondarySkillTree;
    [SerializeField] private bool replaceExisting = true;

    [Header("Tree Switcher")]
    [SerializeField] private bool buildTreeSwitcher = true;
    [SerializeField] private string primarySkillTreeLabel = "CLASS";
    [SerializeField] private string secondarySkillTreeLabel = "ELEMENT";
    [SerializeField] private Vector2 treeSwitcherInset = new(0f, 20f);
    [SerializeField] private Vector2 treeSwitcherSize = new(420f, 56f);
    [SerializeField] private Vector2 treeSwitcherButtonSize = new(200f, 46f);
    [SerializeField] private float treeSwitcherButtonSpacing = 20f;
    [SerializeField] private float treeSwitcherTextSize = 20f;

    [Header("Layout")]
    [SerializeField, Range(0.55f, 0.8f)] private float treeWidthRatio = 0.68f;
    [SerializeField] private float padding = 10f;
    [SerializeField] private float panelGap = 8f;
    [SerializeField] private float treeInnerPadding = 12f;
    [SerializeField] private Vector2 treeClipPadding = new(24f, 24f);
    [SerializeField] private float treeClipHorizontalPadding = 0f;
    [SerializeField] private float treeClipTopPadding = 24f;
    [SerializeField] private float treeClipBottomPadding = 24f;
    [SerializeField, Range(0.45f, 0.85f)] private float detailHeightRatio = 0.68f;
    [SerializeField] private Vector2 contentSize = new(1100f, 860f);
    [SerializeField] private Vector2 contentOffset = Vector2.zero;
    [SerializeField, Range(0.75f, 2.25f)] private float contentScale = 1f;
    [SerializeField] private bool useAuthoredNodePositions = true;
    [SerializeField] private bool useBranchColumnLayout = true;
    [SerializeField] private bool showBranchHeaders = true;
    [SerializeField] private float branchColumnSpacing = 300f;
    [SerializeField] private float branchRowSpacing = 112f;
    [SerializeField] private float branchSiblingSpacing = 86f;
    [SerializeField] private float branchStartY = 160f;
    [SerializeField] private float branchHeaderOffsetY = 92f;
    [SerializeField] private float branchHeaderTextSize = 18f;
    [SerializeField] private bool useBranchLaneLayout = true;
    [SerializeField] private float branchLaneSpacing = 112f;
    [SerializeField] private float branchSameColumnSpacing = 62f;
    [SerializeField] private Vector2 skillPointsInset = new(30f, 54f);
    [SerializeField] private Vector2 skillPointsSize = new(180f, 28f);
    [SerializeField] private float skillPointsTextSize = 16f;
    [SerializeField] private Vector2 resetButtonInset = new(28f, 54f);
    [SerializeField] private Vector2 resetButtonSize = new(92f, 26f);
    [SerializeField] private float resetButtonTextSize = 10f;
    [SerializeField] private bool resetButtonTopRight = false;
    [SerializeField, Range(0.25f, 3f)] private float nodeColumnSpacingScale = 1f;
    [SerializeField, Range(0.25f, 3f)] private float nodeRowSpacingScale = 1f;
    [SerializeField] private float autoColumnSpacing = 130f;
    [SerializeField] private float autoRowSpacing = 70f;
    [SerializeField] private bool autoExpandScrollContent = true;
    [SerializeField] private Vector2 scrollContentPadding = new(160f, 160f);
    [SerializeField] private bool horizontalScroll = false;
    [SerializeField] private bool startTreeAtTop = true;
    [SerializeField] private float scrollSensitivity = 35f;

    [Header("Node")]
    [SerializeField] private float nodeSize = 86f;
    [SerializeField] private float iconSize = 64f;
    [SerializeField] private float lineThickness = 5f;
    [SerializeField] private float textSize = 12f;
    [SerializeField] private float nodeIconFramePadding = 24f;
    [SerializeField] private bool useMajorMinorNodeSizes = true;
    [SerializeField] private float majorNodeSize = 76f;
    [SerializeField] private float majorIconSize = 56f;
    [SerializeField] private float minorNodeSize = 52f;
    [SerializeField] private float minorIconSize = 34f;
    [SerializeField] private bool showNodeRankText = true;
    [SerializeField] private bool showNodeCostText = false;
    [SerializeField] private Vector2 nodeRankTextOffset = new(0f, -38f);
    [SerializeField] private Vector2 nodeCostTextOffset = new(-28f, 28f);

    [Header("Equip Skills")]
    [SerializeField, Range(1, 6)] private int equipSlotCount = 4;
    [SerializeField, Range(0.75f, 2f)] private float equipContentScale = 1.25f;
    [SerializeField] private Vector2 equipSlotSize = new(64f, 64f);
    [SerializeField] private Vector2 equipSlotIconSize = new(48f, 48f);
    [SerializeField] private float equipSlotSpacing = 16f;
    [SerializeField] private float equipIndexSpacingOffset = 0f;
    [SerializeField] private Vector2 equipIndexTextOffset = new(0f, -27f);
    [SerializeField] private float equipIndexTextSize = 8f;
    [SerializeField] private float equipTitleHeight = 28f;
    [SerializeField] private float equipPanelInnerPadding = 14f;

    [Header("Detail Content")]
    [SerializeField, Range(0.75f, 2f)] private float detailContentScale = 1.25f;
    [SerializeField] private Vector2 detailIconSize = new(88f, 88f);
    [SerializeField] private float detailTextWidth = 270f;
    [SerializeField] private Vector2 detailButtonSize = new(126f, 34f);
    [SerializeField] private float detailTitleFontSize = 20f;
    [SerializeField] private float detailBodyFontSize = 14f;
    [SerializeField] private Vector2 detailCostInset = new(16f, 18f);
    [SerializeField] private Vector2 detailCostSize = new(90f, 22f);
    [Header("Text Style")]
    [SerializeField] private TMP_FontAsset textFont;
    [SerializeField] private Color globalTextColor = Color.white;
    [SerializeField] private Color titleTextColor = new(0.9f, 0.96f, 1f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private Color nodeRankTextColor = Color.white;
    [SerializeField] private Color nodeCostTextColor = new(1f, 0.86f, 0.28f, 1f);
    [SerializeField] private float nodeRankTextSize = 8f;
    [SerializeField] private Vector2 nodeRankTextRectSize = new(36f, 14f);
    [SerializeField] private float nodeCostTextSize = 8f;
    [SerializeField] private Vector2 nodeCostTextRectSize = new(24f, 14f);

    [Header("Line Colors")]
    [SerializeField] private Color defaultLineColor = new(0.98f, 0.84f, 0.36f, 0.82f);
    [SerializeField] private Color techniqueLineColor = new(0.95f, 0.76f, 0.32f, 0.82f);
    [SerializeField] private Color defenseLineColor = new(0.36f, 0.75f, 1f, 0.82f);
    [SerializeField] private Color controlLineColor = new(0.42f, 0.95f, 0.64f, 0.82f);
    [SerializeField] private Color fireLineColor = new(1f, 0.38f, 0.24f, 1f);
    [SerializeField] private Color frostLineColor = new(0.55f, 0.9f, 1f, 1f);
    [SerializeField] private Color lightningLineColor = new(0.8f, 0.56f, 1f, 1f);
    [SerializeField] private Color poisonLineColor = new(0.56f, 0.95f, 0.36f, 1f);

    [Header("Sprites")]
    [SerializeField] private Sprite treeFrameSprite;
    [SerializeField] private Sprite detailFrameSprite;
    [SerializeField] private Sprite equipFrameSprite;
    [SerializeField] private Sprite equipIllustrationSprite;
    [SerializeField] private Sprite equipSlotSprite;
    [SerializeField] private Sprite nodeSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite lineSprite;
    [SerializeField] private Sprite buttonSprite;

    private Vector2 scrollPosition;
    private int selectedNodeIndex;
    private Vector2 nodePositionEdit;
    private bool nodePositionLoaded;
    private SkillTreeDefinition lastSkillTree;
    private static TMP_FontAsset activeTextFont;
    private static Color activeTextColor = Color.white;
    private static Color activeButtonTextColor = Color.white;

    [MenuItem("Loot Knights/UI/Skill View Builder")]
    public static void Open()
    {
        GetWindow<SkillViewHierarchyBuilder>(WindowTitle);
    }

    private void OnEnable()
    {
        LoadDefaults();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Skill Tree UI Tool", EditorStyles.boldLabel);
        skillViewRoot = (GameObject)EditorGUILayout.ObjectField("SkillView Root", skillViewRoot, typeof(GameObject), true);
        skillTree = (SkillTreeDefinition)EditorGUILayout.ObjectField("Skill Tree Asset", skillTree, typeof(SkillTreeDefinition), false);
        secondarySkillTree = (SkillTreeDefinition)EditorGUILayout.ObjectField("Secondary Skill Tree", secondarySkillTree, typeof(SkillTreeDefinition), false);
        if (lastSkillTree != skillTree)
            ResetNodePositionEditor();

        replaceExisting = EditorGUILayout.Toggle("Replace Existing", replaceExisting);

        if (skillViewRoot == null && GUILayout.Button("Find SkillView In Scene"))
            skillViewRoot = GameObject.Find("SkillView");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Find Skill Tree Asset"))
            {
                skillTree = FindSkillTreeAsset();
                ResetNodePositionEditor();
            }

            if (GUILayout.Button("Find Elemental Tree"))
                secondarySkillTree = FindElementalSkillTreeAsset();

        }

        DrawFields();
        DrawNodePositionEditor();

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("Build", GUILayout.Height(38f)))
            Build();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Load Saved Defaults", GUILayout.Height(28f)))
                LoadDefaults();

            if (GUILayout.Button("Save Defaults", GUILayout.Height(28f)))
                SaveDefaults();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFields()
    {
        SerializedObject so = new(this);

        DrawGroup(so, "Tree Switcher", nameof(buildTreeSwitcher), nameof(primarySkillTreeLabel), nameof(secondarySkillTreeLabel), nameof(treeSwitcherInset), nameof(treeSwitcherSize), nameof(treeSwitcherButtonSize), nameof(treeSwitcherButtonSpacing), nameof(treeSwitcherTextSize));
        DrawGroup(so, "Layout", nameof(treeWidthRatio), nameof(padding), nameof(panelGap), nameof(treeInnerPadding), nameof(treeClipHorizontalPadding), nameof(treeClipTopPadding), nameof(treeClipBottomPadding), nameof(detailHeightRatio), nameof(contentSize), nameof(contentOffset), nameof(contentScale), nameof(useAuthoredNodePositions), nameof(useBranchColumnLayout), nameof(showBranchHeaders), nameof(branchColumnSpacing), nameof(branchRowSpacing), nameof(branchSiblingSpacing), nameof(branchStartY), nameof(branchHeaderOffsetY), nameof(branchHeaderTextSize), nameof(useBranchLaneLayout), nameof(branchLaneSpacing), nameof(branchSameColumnSpacing), nameof(skillPointsInset), nameof(skillPointsSize), nameof(skillPointsTextSize), nameof(resetButtonInset), nameof(resetButtonSize), nameof(resetButtonTextSize), nameof(resetButtonTopRight), nameof(nodeColumnSpacingScale), nameof(nodeRowSpacingScale), nameof(autoColumnSpacing), nameof(autoRowSpacing), nameof(autoExpandScrollContent), nameof(scrollContentPadding), nameof(horizontalScroll), nameof(startTreeAtTop), nameof(scrollSensitivity));
        DrawGroup(so, "Node", nameof(nodeSize), nameof(iconSize), nameof(lineThickness), nameof(textSize), nameof(nodeIconFramePadding), nameof(useMajorMinorNodeSizes), nameof(majorNodeSize), nameof(majorIconSize), nameof(minorNodeSize), nameof(minorIconSize), nameof(showNodeRankText), nameof(showNodeCostText), nameof(nodeRankTextOffset), nameof(nodeCostTextOffset));
        DrawGroup(so, "Equip Skills", nameof(equipSlotCount), nameof(equipContentScale), nameof(equipSlotSize), nameof(equipSlotIconSize), nameof(equipSlotSpacing), nameof(equipIndexSpacingOffset), nameof(equipIndexTextOffset), nameof(equipIndexTextSize), nameof(equipTitleHeight), nameof(equipPanelInnerPadding));
        DrawGroup(so, "Detail Content", nameof(detailContentScale), nameof(detailIconSize), nameof(detailTextWidth), nameof(detailButtonSize), nameof(detailTitleFontSize), nameof(detailBodyFontSize), nameof(detailCostInset), nameof(detailCostSize));
        DrawGroup(so, "Text Style", nameof(textFont), nameof(globalTextColor), nameof(titleTextColor), nameof(buttonTextColor), nameof(nodeRankTextColor), nameof(nodeCostTextColor), nameof(nodeRankTextSize), nameof(nodeRankTextRectSize), nameof(nodeCostTextSize), nameof(nodeCostTextRectSize));
        DrawGroup(so, "Line Colors", nameof(defaultLineColor), nameof(techniqueLineColor), nameof(defenseLineColor), nameof(controlLineColor), nameof(fireLineColor), nameof(frostLineColor), nameof(lightningLineColor), nameof(poisonLineColor));
        DrawGroup(so, "Sprites", nameof(treeFrameSprite), nameof(detailFrameSprite), nameof(equipFrameSprite), nameof(equipIllustrationSprite), nameof(equipSlotSprite), nameof(nodeSprite), nameof(selectedSprite), nameof(lockedSprite), nameof(lineSprite), nameof(buttonSprite));

        so.ApplyModifiedProperties();
    }

    private void DrawNodePositionEditor()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Node Positions", EditorStyles.boldLabel);

        if (skillTree == null || skillTree.Nodes == null || skillTree.Nodes.Count == 0)
        {
            EditorGUILayout.HelpBox("Choose a Skill Tree Asset to edit node positions.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        string[] nodeNames = GetNodeDisplayNames();
        int safeIndex = Mathf.Clamp(selectedNodeIndex, 0, nodeNames.Length - 1);
        if (safeIndex != selectedNodeIndex || !nodePositionLoaded)
        {
            selectedNodeIndex = safeIndex;
            LoadSelectedNodePosition();
        }

        EditorGUI.BeginChangeCheck();
        selectedNodeIndex = EditorGUILayout.Popup("Node", selectedNodeIndex, nodeNames);
        if (EditorGUI.EndChangeCheck())
            LoadSelectedNodePosition();

        SkillTreeNodeDefinition selectedNode = GetSelectedNode();
        using (new EditorGUI.DisabledScope(selectedNode == null))
        {
            EditorGUI.BeginChangeCheck();
            nodePositionEdit = EditorGUILayout.Vector2Field("Tree Position", nodePositionEdit);
            if (EditorGUI.EndChangeCheck())
                SaveSelectedNodePosition(false);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Left")) NudgeSelectedNode(new Vector2(-10f, 0f));
                if (GUILayout.Button("Right")) NudgeSelectedNode(new Vector2(10f, 0f));
                if (GUILayout.Button("Up")) NudgeSelectedNode(new Vector2(0f, 10f));
                if (GUILayout.Button("Down")) NudgeSelectedNode(new Vector2(0f, -10f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Node Position"))
                    SaveSelectedNodePosition(true);

                if (GUILayout.Button("Select Asset") && selectedNode != null)
                    Selection.activeObject = selectedNode;
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void ResetNodePositionEditor()
    {
        lastSkillTree = skillTree;
        selectedNodeIndex = 0;
        nodePositionLoaded = false;
    }

    private static SkillTreeDefinition FindSkillTreeAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillTreeDefinition");
        if (guids.Length == 0)
        {
            Debug.LogWarning("No SkillTreeDefinition asset found.");
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        foreach (string guid in guids)
        {
            string candidatePath = AssetDatabase.GUIDToAssetPath(guid);
            if (candidatePath.EndsWith("Knight_SkillTree.asset", StringComparison.OrdinalIgnoreCase))
            {
                path = candidatePath;
                break;
            }
        }

        SkillTreeDefinition tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(path);
        Selection.activeObject = tree;
        return tree;
    }

    private static SkillTreeDefinition FindElementalSkillTreeAsset()
    {
        string[] guids = AssetDatabase.FindAssets("Elemental_SkillTree t:SkillTreeDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillTreeDefinition tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(path);
            if (tree != null)
            {
                Selection.activeObject = tree;
                return tree;
            }
        }

        Debug.LogWarning("Elemental_SkillTree.asset was not found.");
        return null;
    }

    private string[] GetNodeDisplayNames()
    {
        string[] names = new string[skillTree.Nodes.Count];
        for (int i = 0; i < names.Length; i++)
        {
            SkillTreeNodeDefinition node = skillTree.Nodes[i];
            names[i] = node != null ? $"{i + 1}. {node.DisplayName}" : $"{i + 1}. Missing Node";
        }

        return names;
    }

    private SkillTreeNodeDefinition GetSelectedNode()
    {
        if (skillTree == null || skillTree.Nodes == null || skillTree.Nodes.Count == 0)
            return null;

        selectedNodeIndex = Mathf.Clamp(selectedNodeIndex, 0, skillTree.Nodes.Count - 1);
        return skillTree.Nodes[selectedNodeIndex];
    }

    private void LoadSelectedNodePosition()
    {
        SkillTreeNodeDefinition node = GetSelectedNode();
        nodePositionEdit = node != null ? node.TreePosition : Vector2.zero;
        nodePositionLoaded = true;
    }

    private void NudgeSelectedNode(Vector2 delta)
    {
        nodePositionEdit += delta;
        SaveSelectedNodePosition(false);
    }

    private void SaveSelectedNodePosition(bool pingAsset)
    {
        SkillTreeNodeDefinition node = GetSelectedNode();
        if (node == null)
            return;

        Undo.RecordObject(node, "Move Skill Tree Node");
        SerializedObject so = new(node);
        so.FindProperty("treePosition").vector2Value = nodePositionEdit;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(node);

        if (pingAsset)
        {
            Selection.activeObject = node;
            EditorGUIUtility.PingObject(node);
        }
    }

    private static void DrawGroup(SerializedObject so, string title, params string[] propertyNames)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, true);
        }

        EditorGUILayout.EndVertical();
    }

    private static void SetObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetString(SerializedObject so, string propertyName, string value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
            property.stringValue = value;
    }

    private static void SetBool(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }

    private void Build()
    {
        if (skillViewRoot == null)
        {
            Debug.LogError("SkillView Root is missing.");
            return;
        }

        if (skillTree == null)
        {
            Debug.LogError("Skill Tree Asset is missing. Click 'Find Skill Tree Asset' in the builder window first.");
            return;
        }

        if (secondarySkillTree == null)
            secondarySkillTree = FindElementalSkillTreeAsset();

        EnsureDefaultSprites();
        activeTextFont = textFont;
        activeTextColor = globalTextColor;
        activeButtonTextColor = buttonTextColor;

        if (!replaceExisting && HasGeneratedContent(skillViewRoot.transform))
        {
            Debug.LogWarning("SkillView already has generated skill tree content.");
            return;
        }

        RemoveGeneratedContent(skillViewRoot.transform);

        RectTransform root = skillViewRoot.GetComponent<RectTransform>();
        if (root == null)
        {
            Debug.LogError("SkillView Root must have a RectTransform.");
            return;
        }

        BuildTree(root);
        BuildDetail(root);
        ConfigureRuntimeView(root);

        EditorSceneManager.MarkSceneDirty(skillViewRoot.scene);
        Debug.Log("Skill tree UI built.");
    }

    private void BuildTree(RectTransform root)
    {
        RectTransform treeArea = CreatePanel(root, "TreeArea", treeFrameSprite);
        Anchor(treeArea, Vector2.zero, new Vector2(treeWidthRatio, 1f), new Vector2(padding, padding), new Vector2(-panelGap * 0.5f, -padding));

        TMP_Text points = CreateText(treeArea, "SkillPointText", "POINTS: 0", Mathf.Max(8f, skillPointsTextSize), TextAlignmentOptions.Right, Vector2.zero, Max(skillPointsSize, new Vector2(80f, 18f)));
        points.color = titleTextColor;
        AnchorTopRight(points.rectTransform, Max(skillPointsInset, Vector2.zero), Max(skillPointsSize, new Vector2(80f, 18f)));

        RectTransform reset = CreateButton(treeArea, "ResetButton", "RESET", Vector2.zero, Max(resetButtonSize, new Vector2(48f, 18f)), Mathf.Max(7f, resetButtonTextSize));
        ApplySprite(reset, buttonSprite);
        if (resetButtonTopRight)
            AnchorTopRight(reset, Max(resetButtonInset, Vector2.zero), Max(resetButtonSize, new Vector2(48f, 18f)));
        else
            AnchorTopLeft(reset, Max(resetButtonInset, Vector2.zero), Max(resetButtonSize, new Vector2(48f, 18f)));

        if (buildTreeSwitcher && HasSecondarySkillTree())
            BuildTreeSwitcher(treeArea);

        RectTransform viewport = CreateEmpty(treeArea, "Viewport");
        float innerPadding = Mathf.Max(0f, treeInnerPadding);
        Anchor(viewport, Vector2.zero, Vector2.one, new Vector2(innerPadding, innerPadding), new Vector2(-innerPadding, -innerPadding));
        AddViewportMask(
            viewport,
            Mathf.Max(0f, treeClipHorizontalPadding),
            Mathf.Max(0f, treeClipTopPadding),
            Mathf.Max(0f, treeClipBottomPadding));

        RectTransform content = CreateEmpty(viewport, "Content");
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);

        List<SkillTreeDefinition> trees = GetBuildSkillTrees();
        Dictionary<SkillTreeDefinition, Dictionary<SkillTreeNodeDefinition, Vector2>> treePositions = new();
        Vector2 scrollContentSize = Vector2.zero;
        foreach (SkillTreeDefinition tree in trees)
        {
            Dictionary<SkillTreeNodeDefinition, Vector2> positions = GetNodePositionsForTree(tree, out Vector2 treeScrollContentSize);
            treePositions[tree] = positions;
            scrollContentSize = Max(scrollContentSize, treeScrollContentSize);
        }

        if (startTreeAtTop)
        {
            foreach (SkillTreeDefinition tree in trees)
            {
                if (tree != skillTree)
                    treePositions[tree] = AlignPositionsToTop(treePositions[tree], scrollContentSize);
            }
        }

        content.sizeDelta = scrollContentSize;
        content.anchoredPosition = Vector2.zero;

        ScrollRect scrollRect = treeArea.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = horizontalScroll;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = Mathf.Max(1f, scrollSensitivity);

        foreach (SkillTreeDefinition tree in trees)
            BuildTreeContent(content, tree, treePositions[tree], tree == skillTree);

        reset.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        scrollRect.horizontalNormalizedPosition = 0.5f;
        scrollRect.verticalNormalizedPosition = startTreeAtTop ? 1f : 0.5f;
    }

    private void BuildTreeContent(
        RectTransform contentRoot,
        SkillTreeDefinition tree,
        IReadOnlyDictionary<SkillTreeNodeDefinition, Vector2> positions,
        bool selected)
    {
        if (tree == null)
            return;

        SkillTreeDefinition previousTree = skillTree;
        skillTree = tree;

        try
        {
            RectTransform treeContent = CreateEmpty(contentRoot, GetTreeContentName(tree));
            Stretch(treeContent);
            treeContent.gameObject.SetActive(selected);

            RectTransform linesRoot = CreateEmpty(treeContent, "Lines");
            Stretch(linesRoot);

            RectTransform headersRoot = CreateEmpty(treeContent, "BranchHeaders");
            Stretch(headersRoot);

            RectTransform nodesRoot = CreateEmpty(treeContent, "Nodes");
            Stretch(nodesRoot);

            CreateBranchHeaders(headersRoot, positions);

            foreach (SkillTreeNodeDefinition node in tree.Nodes)
                CreateNodeLines(linesRoot, node, positions);

            foreach (SkillTreeNodeDefinition node in tree.Nodes)
            {
                if (node != null && positions.TryGetValue(node, out Vector2 position))
                    CreateSkillNode(nodesRoot, node, position);
            }
        }
        finally
        {
            skillTree = previousTree;
        }
    }

    private void BuildTreeSwitcher(Transform treeArea)
    {
        RectTransform switcher = CreateEmpty(treeArea, "TreeSwitcher");
        switcher.anchorMin = new Vector2(0.5f, 1f);
        switcher.anchorMax = new Vector2(0.5f, 1f);
        switcher.pivot = new Vector2(0.5f, 1f);
        switcher.anchoredPosition = new Vector2(treeSwitcherInset.x, -treeSwitcherInset.y);
        switcher.sizeDelta = Max(treeSwitcherSize, new Vector2(120f, 28f));

        Vector2 buttonSize = Max(treeSwitcherButtonSize, new Vector2(54f, 22f));
        float gap = Mathf.Max(0f, treeSwitcherButtonSpacing);
        float offset = buttonSize.x * 0.5f + gap * 0.5f;
        float fontSize = Mathf.Max(7f, treeSwitcherTextSize);

        RectTransform primaryButton = CreateButton(
            switcher,
            "ClassTreeButton",
            string.IsNullOrWhiteSpace(primarySkillTreeLabel) ? "CLASS" : primarySkillTreeLabel,
            new Vector2(-offset, 0f),
            buttonSize,
            fontSize);
        ApplySprite(primaryButton, buttonSprite);

        RectTransform secondaryButton = CreateButton(
            switcher,
            "ElementTreeButton",
            string.IsNullOrWhiteSpace(secondarySkillTreeLabel) ? "ELEMENT" : secondarySkillTreeLabel,
            new Vector2(offset, 0f),
            buttonSize,
            fontSize);
        ApplySprite(secondaryButton, buttonSprite);
    }

    private static bool HasGeneratedContent(Transform root)
    {
        if (root == null)
            return false;

        foreach (string childName in GeneratedRootNames)
        {
            if (root.Find(childName) != null)
                return true;
        }

        return false;
    }

    private static void RemoveGeneratedContent(Transform root)
    {
        if (root == null)
            return;

        foreach (string childName in GeneratedRootNames)
        {
            Transform child = root.Find(childName);
            if (child == null)
                continue;

            RepairMissingCanvasRenderers(child);
            DestroyImmediate(child.gameObject);
        }
    }


    private void BuildDetail(RectTransform root)
    {
        RectTransform detail = CreatePanel(root, "DetailPanel", detailFrameSprite);
        Anchor(detail, new Vector2(treeWidthRatio, 1f - detailHeightRatio), Vector2.one, new Vector2(panelGap * 0.5f, panelGap * 0.5f), new Vector2(-padding, -padding));

        Sprite previewIcon = GetFirstIcon();
        float scale = Mathf.Max(0.1f, detailContentScale);
        float textWidth = Mathf.Max(120f, detailTextWidth);
        Vector2 iconSizeValue = Max(detailIconSize, new Vector2(24f, 24f)) * scale;
        Vector2 buttonSizeValue = Max(detailButtonSize, new Vector2(64f, 24f)) * scale;
        float titleFont = Mathf.Max(8f, detailTitleFontSize * scale);
        float bodyFont = Mathf.Max(7f, detailBodyFontSize * scale);

        CreateCircularIcon(detail, "SkillIcon", previewIcon, Scale(new Vector2(0f, 88f), scale), iconSizeValue, true);
        TMP_Text skillName = CreateText(detail, "SkillNameText", "Select Skill", titleFont, TextAlignmentOptions.Center, Scale(new Vector2(0f, 30f), scale), Scale(new Vector2(textWidth, 30f), scale));
        skillName.color = titleTextColor;
        CreateText(detail, "RankText", "RANK 0/0", bodyFont, TextAlignmentOptions.Center, Scale(new Vector2(0f, 2f), scale), Scale(new Vector2(textWidth, 24f), scale));
        TMP_Text description = CreateText(detail, "DescriptionText", "Skill description", bodyFont, TextAlignmentOptions.TopLeft, Scale(new Vector2(0f, -48f), scale), Scale(new Vector2(textWidth, 88f), scale));
        description.enableAutoSizing = true;
        description.fontSizeMin = Mathf.Max(7f, bodyFont * 0.68f);
        description.fontSizeMax = bodyFont;
        description.overflowMode = TextOverflowModes.Ellipsis;
        CreateText(detail, "RequirementText", "Requirement", Mathf.Max(7f, (detailBodyFontSize - 1f) * scale), TextAlignmentOptions.Center, Scale(new Vector2(0f, -108f), scale), Scale(new Vector2(textWidth, 22f), scale));

        TMP_Text costText = CreateText(detail, "CostText", "Cost: 0", Mathf.Max(7f, (detailBodyFontSize - 1f) * scale), TextAlignmentOptions.Right, Vector2.zero, Max(detailCostSize, new Vector2(60f, 18f)) * scale);
        AnchorTopRight(costText.rectTransform, Max(detailCostInset, Vector2.zero) * scale, Max(detailCostSize, new Vector2(60f, 18f)) * scale);

        Vector2 actionButtonSize = new(buttonSizeValue.x * 0.78f, buttonSizeValue.y);
        float actionGap = 6f * scale;
        float actionOffset = actionButtonSize.x * 0.5f + actionGap * 0.5f;

        RectTransform upgrade = CreateButton(detail, "UpgradeButton", "UPGRADE", Scale(new Vector2(-actionOffset / scale, -136f), scale), actionButtonSize, Mathf.Max(8f, 12f * scale));
        ApplySprite(upgrade, buttonSprite);

        RectTransform equip = CreateButton(detail, "EquipButton", "EQUIP", Scale(new Vector2(actionOffset / scale, -136f), scale), actionButtonSize, Mathf.Max(8f, 12f * scale));
        ApplySprite(equip, buttonSprite);

        BuildEquipPanel(root);
    }

    private void BuildEquipPanel(RectTransform root)
    {
        RectTransform equipPanel = CreatePanel(root, "EquipSkillPanel", equipFrameSprite);
        Anchor(equipPanel, new Vector2(treeWidthRatio, 0f), new Vector2(1f, 1f - detailHeightRatio), new Vector2(panelGap * 0.5f, padding), new Vector2(-padding, -panelGap * 0.5f));

        float scale = Mathf.Max(0.1f, equipContentScale);
        float innerPadding = Mathf.Max(0f, equipPanelInnerPadding * scale);
        float titleHeight = Mathf.Max(12f, equipTitleHeight * scale);
        float slotSpacing = Mathf.Max(0f, equipSlotSpacing * scale);
        Vector2 slotSize = Max(equipSlotSize, new Vector2(24f, 24f)) * scale;
        Vector2 slotIconSize = Max(equipSlotIconSize, new Vector2(16f, 16f)) * scale;
        float indexSpacingOffset = equipIndexSpacingOffset * scale;
        Vector2 indexTextOffset = equipIndexTextOffset * scale;
        float indexTextSize = Mathf.Max(6f, equipIndexTextSize * scale);

        TMP_Text title = CreateText(equipPanel, "TitleText", "EQUIP SKILLS", Mathf.Max(8f, 13f * scale), TextAlignmentOptions.Center, Vector2.zero, new Vector2(260f * scale, titleHeight));
        title.color = titleTextColor;
        Anchor(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(innerPadding, -innerPadding - titleHeight),
            new Vector2(-innerPadding, -innerPadding));

        if (equipIllustrationSprite != null)
        {
            RectTransform illustrationIcon = CreateCircularIcon(equipPanel, "EquipIllustrationIcon", equipIllustrationSprite, Vector2.zero, new Vector2(32f, 32f) * scale, false);
            AnchorTopRight(illustrationIcon, new Vector2(innerPadding, innerPadding + 2f * scale), new Vector2(32f, 32f) * scale);
        }

        CreateElementCoreSlot(equipPanel, slotSize, slotIconSize, indexTextOffset, indexTextSize, scale);

        RectTransform slotsRoot = CreateEmpty(equipPanel, "Slots");
        float titleGap = Mathf.Max(4f, 8f * scale);
        Anchor(slotsRoot, Vector2.zero, Vector2.one, new Vector2(innerPadding, innerPadding), new Vector2(-innerPadding, -innerPadding - titleHeight - titleGap));

        int count = Mathf.Clamp(equipSlotCount, 1, 6);
        float totalWidth = count * slotSize.x + (count - 1) * slotSpacing;
        float startX = -totalWidth * 0.5f + slotSize.x * 0.5f;

        for (int i = 0; i < count; i++)
            CreateEquipSlot(slotsRoot, i, count, new Vector2(startX + i * (slotSize.x + slotSpacing), 0f), slotSize, slotIconSize, scale, indexSpacingOffset, indexTextOffset, indexTextSize);
    }

    private void CreateEquipSlot(Transform parent, int index, int count, Vector2 position, Vector2 slotSize, Vector2 slotIconSize, float scale, float indexSpacingOffset, Vector2 indexTextOffset, float indexTextSize)
    {
        RectTransform slot = CreateImage(parent, $"EquipSlot_{index + 1}", equipSlotSprite, position, slotSize);
        slot.gameObject.AddComponent<SkillTreeEquipSlotDropTarget>();
        RectTransform mask = CreateCircleMask(slot, "IconMask", Vector2.zero, slotIconSize);
        CreateImage(mask, "Icon", null, Vector2.zero, slotIconSize);

        float centerIndex = (Mathf.Max(1, count) - 1) * 0.5f;
        Vector2 labelPosition = indexTextOffset + new Vector2((index - centerIndex) * indexSpacingOffset, 0f);
        CreateText(slot, "IndexText", (index + 1).ToString(), indexTextSize, TextAlignmentOptions.Center, labelPosition, new Vector2(28f * scale, 14f * scale));
    }

    private void CreateElementCoreSlot(RectTransform parent, Vector2 slotSize, Vector2 slotIconSize, Vector2 indexTextOffset, float indexTextSize, float scale)
    {
        RectTransform slot = CreateImage(parent, "ElementCoreSlot", null, Vector2.zero, slotSize);
        slot.anchorMin = new Vector2(0.5f, 0.5f);
        slot.anchorMax = new Vector2(0.5f, 0.5f);
        slot.pivot = new Vector2(0.5f, 0.5f);
        slot.anchoredPosition = Vector2.zero;

        Image background = slot.GetComponent<Image>();
        if (background != null)
        {
            background.color = Color.clear;
            background.raycastTarget = false;
        }

        RectTransform frame = CreateImage(slot, "NodeFrame", nodeSprite, Vector2.zero, slotIconSize + new Vector2(30f * scale, 30f * scale));
        Image frameImage = frame.GetComponent<Image>();
        if (frameImage != null)
        {
            frameImage.color = Color.white;
            frameImage.preserveAspect = true;
            frameImage.raycastTarget = false;
        }

        RectTransform mask = CreateCircleMask(slot, "IconMask", Vector2.zero, slotIconSize);
        CreateImage(mask, "Icon", null, Vector2.zero, slotIconSize);

        TMP_Text label = CreateText(
            slot,
            "LabelText",
            "ELEMENT",
            indexTextSize,
            TextAlignmentOptions.Center,
            indexTextOffset,
            new Vector2(Mathf.Max(28f * scale, slotSize.x), 14f * scale));
        label.color = new Color(0.45f, 1f, 0.95f, 1f);
        slot.gameObject.SetActive(false);
    }

    private Dictionary<SkillTreeNodeDefinition, Vector2> GetNodePositionsForTree(
        SkillTreeDefinition tree,
        out Vector2 scrollSize)
    {
        SkillTreeDefinition previousTree = skillTree;
        skillTree = tree;

        try
        {
            return GetNodePositions(out scrollSize);
        }
        finally
        {
            skillTree = previousTree;
        }
    }

    private Dictionary<SkillTreeNodeDefinition, Vector2> GetNodePositions(out Vector2 scrollSize)
    {
        if (!useAuthoredNodePositions && useBranchColumnLayout)
            return GetBranchColumnNodePositions(out scrollSize);

        Dictionary<SkillTreeNodeDefinition, Vector2> positions = new();
        Dictionary<SkillTreeBranch, int> branchRows = new();

        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null) continue;

            Vector2 position = node.TreePosition;
            if (IsZero(position))
                position = GetAutoPosition(node.Branch, branchRows);

            Vector2 spacedPosition = new(
                position.x * Mathf.Max(0.01f, nodeColumnSpacingScale),
                position.y * Mathf.Max(0.01f, nodeRowSpacingScale));

            positions[node] = spacedPosition * contentScale + contentOffset;
        }

        scrollSize = GetScrollContentSize(positions);
        return startTreeAtTop ? AlignPositionsToTop(positions, scrollSize) : positions;
    }

    private Dictionary<SkillTreeNodeDefinition, Vector2> GetBranchColumnNodePositions(out Vector2 scrollSize)
    {
        Dictionary<SkillTreeNodeDefinition, Vector2> positions = new();
        List<SkillTreeBranch> branches = GetVisibleBranches();
        Dictionary<SkillTreeBranch, int> branchColumns = new();
        for (int i = 0; i < branches.Count; i++)
            branchColumns[branches[i]] = i;

        Dictionary<SkillTreeNodeDefinition, int> depths = new();
        Dictionary<BranchDepthKey, List<SkillTreeNodeDefinition>> groups = new();

        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null) continue;

            int depth = GetNodeDepth(node, depths, new HashSet<SkillTreeNodeDefinition>());
            BranchDepthKey key = new BranchDepthKey(node.Branch, depth);
            if (!groups.TryGetValue(key, out List<SkillTreeNodeDefinition> group))
            {
                group = new List<SkillTreeNodeDefinition>();
                groups[key] = group;
            }

            group.Add(node);
        }

        foreach (KeyValuePair<BranchDepthKey, List<SkillTreeNodeDefinition>> pair in groups)
        {
            List<SkillTreeNodeDefinition> group = pair.Value;
            group.Sort(CompareNodesForBranchLane);

            int columnIndex = branchColumns.TryGetValue(pair.Key.Branch, out int index) ? index : 0;
            float baseX = (columnIndex - (branches.Count - 1) * 0.5f) * Mathf.Max(1f, branchColumnSpacing);
            float y = branchStartY - pair.Key.Depth * Mathf.Max(1f, branchRowSpacing);
            Dictionary<int, int> laneRows = new();

            for (int i = 0; i < group.Count; i++)
            {
                int lane = GetBranchLane(group[i], pair.Key.Depth);
                laneRows.TryGetValue(lane, out int laneRow);
                laneRows[lane] = laneRow + 1;

                float siblingOffset = GetBranchLaneOffset(lane, i, group.Count);
                float rowOffset = GetBranchLaneRowOffset(laneRow);
                Vector2 rawPosition = new Vector2(baseX + siblingOffset, y - rowOffset);
                positions[group[i]] = rawPosition * contentScale + contentOffset;
            }
        }

        scrollSize = GetScrollContentSize(positions);
        return startTreeAtTop ? AlignPositionsToTop(positions, scrollSize) : positions;
    }

    private int CompareNodesForBranchLane(SkillTreeNodeDefinition a, SkillTreeNodeDefinition b)
    {
        int laneCompare = GetBranchLane(a, 0).CompareTo(GetBranchLane(b, 0));
        if (laneCompare != 0)
            return laneCompare;

        return GetNodeOrder(a).CompareTo(GetNodeOrder(b));
    }

    private float GetBranchLaneOffset(int lane, int index, int count)
    {
        if (!useBranchLaneLayout)
            return (index - (count - 1) * 0.5f) * Mathf.Max(0f, branchSiblingSpacing);

        float laneSpacing = Mathf.Max(0f, branchLaneSpacing);
        return (lane == 0 ? -0.5f : 0.5f) * laneSpacing;
    }

    private float GetBranchLaneRowOffset(int laneRow)
    {
        if (!useBranchLaneLayout || laneRow <= 0)
            return 0f;

        return laneRow * Mathf.Max(1f, branchSameColumnSpacing);
    }

    private int GetBranchLane(SkillTreeNodeDefinition node, int depth)
    {
        if (useBranchLaneLayout && node != null && !IsZero(node.TreePosition))
        {
            float branchCenterX = GetAuthoredBranchCenterX(node.Branch);
            return node.TreePosition.x < branchCenterX ? 0 : 1;
        }

        int branchOrder = GetBranchNodeOrder(node);
        if (branchOrder >= 0)
            return branchOrder % 2;

        return Mathf.Abs(depth) % 2;
    }

    private float GetAuthoredBranchCenterX(SkillTreeBranch branch)
    {
        if (skillTree == null)
            return 0f;

        float min = 0f;
        float max = 0f;
        bool hasPosition = false;

        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null || node.Branch != branch || IsZero(node.TreePosition))
                continue;

            float x = node.TreePosition.x;
            if (!hasPosition)
            {
                min = x;
                max = x;
                hasPosition = true;
                continue;
            }

            min = Mathf.Min(min, x);
            max = Mathf.Max(max, x);
        }

        return hasPosition ? (min + max) * 0.5f : 0f;
    }

    private int GetBranchNodeOrder(SkillTreeNodeDefinition node)
    {
        if (skillTree == null || node == null)
            return -1;

        int order = 0;
        foreach (SkillTreeNodeDefinition candidate in skillTree.Nodes)
        {
            if (candidate == null || candidate.Branch != node.Branch)
                continue;

            if (candidate == node)
                return order;

            order++;
        }

        return -1;
    }

    private int GetNodeDepth(
        SkillTreeNodeDefinition node,
        Dictionary<SkillTreeNodeDefinition, int> depths,
        HashSet<SkillTreeNodeDefinition> visiting)
    {
        if (node == null)
            return 0;

        if (depths.TryGetValue(node, out int cachedDepth))
            return cachedDepth;

        if (!visiting.Add(node))
            return 0;

        int depth = 0;
        foreach (SkillTreePrerequisite prerequisite in node.Prerequisites)
        {
            SkillTreeNodeDefinition prerequisiteNode = prerequisite?.Node;
            if (prerequisiteNode == null)
                continue;

            depth = Mathf.Max(depth, GetNodeDepth(prerequisiteNode, depths, visiting) + 1);
        }

        visiting.Remove(node);
        depths[node] = depth;
        return depth;
    }

    private List<SkillTreeBranch> GetVisibleBranches()
    {
        List<SkillTreeBranch> branches = new();
        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null || branches.Contains(node.Branch))
                continue;

            branches.Add(node.Branch);
        }

        branches.Sort((a, b) => GetBranchColumn(a).CompareTo(GetBranchColumn(b)));
        if (branches.Count == 0)
            branches.Add(SkillTreeBranch.KnightDefense);

        return branches;
    }

    private int GetNodeOrder(SkillTreeNodeDefinition node)
    {
        if (skillTree == null || node == null)
            return int.MaxValue;

        for (int i = 0; i < skillTree.Nodes.Count; i++)
        {
            if (skillTree.Nodes[i] == node)
                return i;
        }

        return int.MaxValue;
    }

    private Vector2 GetScrollContentSize(IReadOnlyDictionary<SkillTreeNodeDefinition, Vector2> positions)
    {
        Vector2 size = new(Mathf.Max(1f, contentSize.x), Mathf.Max(1f, contentSize.y));
        if (!autoExpandScrollContent || positions == null || positions.Count == 0)
            return size;

        bool hasPosition = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        foreach (Vector2 position in positions.Values)
        {
            if (!hasPosition)
            {
                min = position;
                max = position;
                hasPosition = true;
                continue;
            }

            min = Vector2.Min(min, position);
            max = Vector2.Max(max, position);
        }

        if (!hasPosition)
            return size;

        Vector2 boundsSize = startTreeAtTop
            ? max - min + new Vector2(GetMaxNodeFrameSize(), GetMaxNodeFrameSize()) + scrollContentPadding * 2f
            : Vector2.Max(Abs(min), Abs(max)) * 2f + new Vector2(GetMaxNodeFrameSize(), GetMaxNodeFrameSize()) + scrollContentPadding * 2f;

        return Vector2.Max(size, boundsSize);
    }

    private Dictionary<SkillTreeNodeDefinition, Vector2> AlignPositionsToTop(
        IReadOnlyDictionary<SkillTreeNodeDefinition, Vector2> positions,
        Vector2 scrollSize)
    {
        Dictionary<SkillTreeNodeDefinition, Vector2> aligned = new();
        if (positions == null || positions.Count == 0)
            return aligned;

        bool hasPosition = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        foreach (Vector2 position in positions.Values)
        {
            if (!hasPosition)
            {
                min = position;
                max = position;
                hasPosition = true;
                continue;
            }

            min = Vector2.Min(min, position);
            max = Vector2.Max(max, position);
        }

        float targetTopY = scrollSize.y * 0.5f - scrollContentPadding.y - GetMaxNodeFrameSize() * 0.5f;
        float yOffset = targetTopY - max.y;
        float xOffset = -(min.x + max.x) * 0.5f;

        foreach (KeyValuePair<SkillTreeNodeDefinition, Vector2> pair in positions)
            aligned[pair.Key] = pair.Value + new Vector2(xOffset, yOffset);

        return aligned;
    }

    private static Vector2 Abs(Vector2 value)
    {
        return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }

    private Vector2 GetAutoPosition(SkillTreeBranch branch, Dictionary<SkillTreeBranch, int> branchRows)
    {
        branchRows.TryGetValue(branch, out int row);
        branchRows[branch] = row + 1;

        float column = GetBranchColumn(branch);
        return new Vector2((column - 3.5f) * autoColumnSpacing, 180f - row * autoRowSpacing);
    }

    private static float GetBranchColumn(SkillTreeBranch branch)
    {
        return branch switch
        {
            SkillTreeBranch.KnightTechnique => 0f,
            SkillTreeBranch.KnightDefense => 1f,
            SkillTreeBranch.KnightControl => 2f,
            SkillTreeBranch.GauntletFire => 3f,
            SkillTreeBranch.GauntletFrost => 4f,
            SkillTreeBranch.GauntletLightning => 5f,
            SkillTreeBranch.GauntletPoison => 6f,
            SkillTreeBranch.GauntletReaction => 7f,
            _ => 0f
        };
    }

    private void CreateNodeLines(Transform parent, SkillTreeNodeDefinition node, IReadOnlyDictionary<SkillTreeNodeDefinition, Vector2> positions)
    {
        if (node == null || !positions.TryGetValue(node, out Vector2 to))
            return;

        foreach (SkillTreePrerequisite prerequisite in node.Prerequisites)
        {
            SkillTreeNodeDefinition fromNode = prerequisite?.Node;
            if (fromNode == null || !positions.TryGetValue(fromNode, out Vector2 from))
                continue;

            CreateLine(parent, $"Line_{fromNode.NodeId}_To_{node.NodeId}", from, to, GetConnectionLineColor(fromNode, node));
        }
    }

    private void CreateBranchHeaders(Transform parent, IReadOnlyDictionary<SkillTreeNodeDefinition, Vector2> positions)
    {
        if (useAuthoredNodePositions || !useBranchColumnLayout || !showBranchHeaders || positions == null || positions.Count == 0)
            return;

        List<SkillTreeBranch> branches = GetVisibleBranches();
        foreach (SkillTreeBranch branch in branches)
        {
            bool hasNode = false;
            float xSum = 0f;
            int count = 0;
            float maxY = float.MinValue;

            foreach (KeyValuePair<SkillTreeNodeDefinition, Vector2> pair in positions)
            {
                if (pair.Key == null || pair.Key.Branch != branch)
                    continue;

                hasNode = true;
                xSum += pair.Value.x;
                count++;
                maxY = Mathf.Max(maxY, pair.Value.y);
            }

            if (!hasNode || count <= 0)
                continue;

            Vector2 position = new Vector2(xSum / count, maxY + branchHeaderOffsetY * contentScale);
            TMP_Text header = CreateText(
                parent,
                $"Header_{branch}",
                GetBranchDisplayName(branch),
                Mathf.Max(8f, branchHeaderTextSize * contentScale),
                TextAlignmentOptions.Center,
                position,
                new Vector2(Mathf.Max(120f, branchColumnSpacing * 0.9f) * contentScale, 38f * contentScale));

            header.fontStyle = FontStyles.Bold;
            header.color = new Color(0.9f, 0.96f, 1f, 0.9f);
        }
    }

    private void CreateSkillNode(Transform parent, SkillTreeNodeDefinition definition, Vector2 position)
    {
        float frameSize = GetNodeFrameSize(definition);
        float iconSizeValue = Mathf.Min(GetNodeIconSize(definition), Mathf.Max(1f, frameSize - Mathf.Max(0f, nodeIconFramePadding) * 2f));

        RectTransform node = CreatePanel(parent, $"Node_{SanitizeName(definition.NodeId)}", nodeSprite);
        SetCentered(node, position, new Vector2(frameSize, frameSize));

        Image nodeImage = node.GetComponent<Image>();
        if (nodeImage != null)
        {
            nodeImage.type = Image.Type.Simple;
            nodeImage.preserveAspect = false;
            nodeImage.raycastTarget = true;
        }

        Button nodeButton = node.gameObject.AddComponent<Button>();
        if (nodeImage != null)
        {
            nodeImage.raycastTarget = true;
            nodeButton.targetGraphic = nodeImage;
        }

        Sprite icon = definition.Icon != null ? definition.Icon : definition.ActiveSkill != null ? definition.ActiveSkill.Icon : null;
        if (icon != null)
        {
            if (!IsElementalTreeNode(definition))
                CreateCircleGraphic(node, "IconBack", Vector2.zero, new Vector2(iconSizeValue, iconSizeValue), new Color(0.02f, 0.04f, 0.08f, 0.92f));

            RectTransform mask = CreateCircleMask(node, "IconMask", Vector2.zero, new Vector2(iconSizeValue, iconSizeValue));
            RectTransform iconRect = CreateImage(mask, "Icon", icon, Vector2.zero, new Vector2(iconSizeValue, iconSizeValue));
            Image iconImage = iconRect.GetComponent<Image>();
            if (iconImage != null && IsElementalTreeNode(definition))
                iconImage.color = GetNodeAccentColor(definition);
        }
        else
        {
            CreateText(node, "Label", GetShortLabel(definition), textSize, TextAlignmentOptions.Center, Vector2.zero, new Vector2(iconSizeValue, iconSizeValue));
        }

        if (showNodeRankText)
        {
            TMP_Text rank = CreateText(node, "RankText", $"0/{definition.MaxRank}", Mathf.Max(1f, nodeRankTextSize), TextAlignmentOptions.Center, nodeRankTextOffset, Max(nodeRankTextRectSize, new Vector2(8f, 6f)));
            rank.color = nodeRankTextColor;
        }

        if (showNodeCostText)
        {
            TMP_Text cost = CreateText(node, "CostText", definition.PointCost.ToString(), Mathf.Max(1f, nodeCostTextSize), TextAlignmentOptions.Center, nodeCostTextOffset, Max(nodeCostTextRectSize, new Vector2(8f, 6f)));
            cost.color = nodeCostTextColor;
        }

        CreateImage(node, "LockOverlay", lockedSprite, Vector2.zero, new Vector2(frameSize, frameSize)).gameObject.SetActive(false);
        CreateImage(node, "SelectedFrame", selectedSprite, Vector2.zero, new Vector2(frameSize + 8f, frameSize + 8f)).gameObject.SetActive(false);

        SkillTreeNodeView nodeView = node.gameObject.AddComponent<SkillTreeNodeView>();
        nodeView.Bind(null, definition);
    }

    private bool IsMajorNode(SkillTreeNodeDefinition definition)
    {
        if (!useMajorMinorNodeSizes || definition == null)
            return true;

        return definition.Kind == SkillTreeNodeKind.ActiveSkill ||
               definition.Kind == SkillTreeNodeKind.SkillUpgrade ||
               definition.ActiveSkill != null ||
               definition.PointCost > 1 ||
               definition.Prerequisites == null ||
               definition.Prerequisites.Count == 0;
    }

    private float GetNodeFrameSize(SkillTreeNodeDefinition definition)
    {
        if (!useMajorMinorNodeSizes)
            return Mathf.Max(1f, nodeSize);

        return Mathf.Max(1f, IsMajorNode(definition) ? majorNodeSize : minorNodeSize);
    }

    private float GetNodeIconSize(SkillTreeNodeDefinition definition)
    {
        if (!useMajorMinorNodeSizes)
            return Mathf.Max(1f, iconSize);

        return Mathf.Max(1f, IsMajorNode(definition) ? majorIconSize : minorIconSize);
    }

    private float GetMaxNodeFrameSize()
    {
        if (!useMajorMinorNodeSizes)
            return Mathf.Max(1f, nodeSize);

        return Mathf.Max(1f, Mathf.Max(majorNodeSize, minorNodeSize));
    }

    private static string GetBranchDisplayName(SkillTreeBranch branch)
    {
        return branch switch
        {
            SkillTreeBranch.KnightTechnique => "TECHNIQUE",
            SkillTreeBranch.KnightDefense => "DEFENSE",
            SkillTreeBranch.KnightControl => "CONTROL",
            SkillTreeBranch.GauntletFire => "FIRE",
            SkillTreeBranch.GauntletFrost => "FROST",
            SkillTreeBranch.GauntletLightning => "LIGHTNING",
            SkillTreeBranch.GauntletPoison => "POISON",
            SkillTreeBranch.GauntletReaction => "REACTION",
            _ => branch.ToString().ToUpperInvariant()
        };
    }

    private List<SkillTreeDefinition> GetBuildSkillTrees()
    {
        List<SkillTreeDefinition> trees = new();
        if (skillTree != null)
            trees.Add(skillTree);

        if (HasSecondarySkillTree())
            trees.Add(secondarySkillTree);

        return trees;
    }

    private bool HasSecondarySkillTree()
    {
        return secondarySkillTree != null && secondarySkillTree != skillTree;
    }

    private static string GetTreeContentName(SkillTreeDefinition tree)
    {
        return $"TreeContent_{SanitizeTreeContentName(tree != null ? tree.TreeId : "Tree")}";
    }

    private void ConfigureRuntimeView(RectTransform root)
    {
        SkillTreeView view = skillViewRoot.GetComponent<SkillTreeView>();
        if (view == null)
            view = skillViewRoot.AddComponent<SkillTreeView>();

        SkillTreePresenter presenter = skillViewRoot.GetComponent<SkillTreePresenter>();
        if (presenter == null)
            presenter = skillViewRoot.AddComponent<SkillTreePresenter>();

        ConfigureRuntimeSerializedFields(view, presenter);

        foreach (SkillTreeNodeView nodeView in root.GetComponentsInChildren<SkillTreeNodeView>(true))
        {
            if (nodeView == null || nodeView.Definition == null) continue;
            view.RegisterNode(nodeView, nodeView.Definition);
        }
    }

    private void ConfigureRuntimeSerializedFields(SkillTreeView view, SkillTreePresenter presenter)
    {
        if (view != null)
        {
            SerializedObject viewSo = new(view);
            SetObjectReference(viewSo, "skillTree", skillTree);
            SetObjectReference(viewSo, "primarySkillTree", skillTree);
            SetObjectReference(viewSo, "secondarySkillTree", secondarySkillTree);
            SetString(viewSo, "primarySkillTreeLabel", primarySkillTreeLabel);
            SetString(viewSo, "secondarySkillTreeLabel", secondarySkillTreeLabel);
            SetBool(viewSo, "buildMissingNodeViews", false);
            SetBool(viewSo, "buildTreeSwitcher", false);
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        if (presenter != null)
        {
            SerializedObject presenterSo = new(presenter);
            SetObjectReference(presenterSo, "view", view);
            SetObjectReference(presenterSo, "skillTree", skillTree);
            presenterSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }
    }

    private void CreateLine(Transform parent, string name, Vector2 from, Vector2 to, Color color)
    {
        Vector2 middle = (from + to) * 0.5f;
        float length = Vector2.Distance(from, to);
        RectTransform line = CreatePanel(parent, name, lineSprite);
        SetCentered(line, middle, new Vector2(Mathf.Max(1f, lineThickness), Mathf.Max(1f, length)));

        Image image = line.GetComponent<Image>();
        if (image != null)
        {
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            if (color.a <= 0f)
                color.a = 1f;

            image.color = color;
        }

        Vector2 direction = to - from;
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    private Color GetLineColor(SkillTreeBranch branch)
    {
        return branch switch
        {
            SkillTreeBranch.KnightTechnique => techniqueLineColor,
            SkillTreeBranch.KnightDefense => defenseLineColor,
            SkillTreeBranch.KnightControl => controlLineColor,
            SkillTreeBranch.GauntletFire => fireLineColor,
            SkillTreeBranch.GauntletFrost => frostLineColor,
            SkillTreeBranch.GauntletLightning => lightningLineColor,
            SkillTreeBranch.GauntletPoison => poisonLineColor,
            _ => defaultLineColor
        };
    }

    private Color GetConnectionLineColor(SkillTreeNodeDefinition fromNode, SkillTreeNodeDefinition toNode)
    {
        if (toNode != null && toNode.Kind == SkillTreeNodeKind.ElementReaction && fromNode != null)
            return GetNodeAccentColor(fromNode);

        return GetLineColor(toNode != null ? toNode.Branch : SkillTreeBranch.GauntletReaction);
    }

    private Color GetNodeAccentColor(SkillTreeNodeDefinition definition)
    {
        if (definition == null)
            return defaultLineColor;

        return definition.Element switch
        {
            ElementType.Fire => fireLineColor,
            ElementType.Frost => frostLineColor,
            ElementType.Lightning => lightningLineColor,
            ElementType.Poison => poisonLineColor,
            _ => definition.Kind == SkillTreeNodeKind.ElementReaction
                ? defaultLineColor
                : GetLineColor(definition.Branch)
        };
    }

    private static bool IsElementalTreeNode(SkillTreeNodeDefinition definition)
    {
        return definition != null &&
               (definition.Kind == SkillTreeNodeKind.ElementUnlock ||
                definition.Kind == SkillTreeNodeKind.ElementReaction ||
                definition.Element != ElementType.None ||
                definition.Reaction != ElementalReactionType.None);
    }

    private Sprite GetFirstIcon()
    {
        foreach (SkillTreeNodeDefinition node in skillTree.Nodes)
        {
            if (node == null) continue;
            if (node.Icon != null) return node.Icon;
            if (node.ActiveSkill != null && node.ActiveSkill.Icon != null) return node.ActiveSkill.Icon;
        }

        return null;
    }

    private void EnsureDefaultSprites()
    {
        treeFrameSprite ??= LoadSprite("Assets/_ThirdParty/Cyberpunk RPG GUI Pack/Cyberpunk RPG GUI Resources/SlicedElements/04Hero_Attribute/bg_RoleDecription.png");
        detailFrameSprite ??= treeFrameSprite;
        equipFrameSprite ??= treeFrameSprite;
        nodeSprite ??= LoadSprite("Assets/_ThirdParty/Cyberpunk RPG GUI Pack/Cyberpunk RPG GUI Resources/SlicedElements/03Scene2/Btn_Skill_n.png");
        equipSlotSprite ??= nodeSprite;
        selectedSprite ??= LoadSprite("Assets/_ThirdParty/Cyberpunk RPG GUI Pack/Cyberpunk RPG GUI Resources/SlicedElements/03Scene2/Btn_Skill_c.png");
        lockedSprite ??= LoadSprite("Assets/_ThirdParty/Cyberpunk RPG GUI Pack/Cyberpunk RPG GUI Resources/SlicedElements/Icons4/Icon_Locked.png");
        lineSprite ??= LoadSprite("Assets/_ThirdParty/Cyberpunk RPG GUI Pack/Cyberpunk RPG GUI Resources/SlicedElements/04Hero_Attribute/Line.png");
        buttonSprite ??= LoadSprite("Assets/_ThirdParty/Cyberpunk RPG GUI Pack/Cyberpunk RPG GUI Resources/SlicedElements/05Hero_Equipment/Btn_Blue_n.png");
    }

    private void SaveDefaults()
    {
        string json = EditorJsonUtility.ToJson(this);
        EditorPrefs.SetString(DefaultsKey, json);
        foreach (string legacyKey in LegacyDefaultsKeys)
            EditorPrefs.DeleteKey(legacyKey);

        Debug.Log("Skill View Builder defaults saved.");
    }

    private void LoadDefaults()
    {
        string key = GetSavedDefaultsKey();
        if (string.IsNullOrEmpty(key))
        {
            ApplyPolishedDefaults();
            Repaint();
            return;
        }

        EditorJsonUtility.FromJsonOverwrite(EditorPrefs.GetString(key), this);
        if (key != DefaultsKey)
            EditorPrefs.SetString(DefaultsKey, EditorPrefs.GetString(key));

        Repaint();
    }

    private static string GetSavedDefaultsKey()
    {
        if (EditorPrefs.HasKey(DefaultsKey))
            return DefaultsKey;

        foreach (string legacyKey in LegacyDefaultsKeys)
        {
            if (EditorPrefs.HasKey(legacyKey))
                return legacyKey;
        }

        return null;
    }

    private void ApplyPolishedDefaults()
    {
        contentSize = new Vector2(1100f, 860f);
        contentOffset = Vector2.zero;
        contentScale = 1f;
        useAuthoredNodePositions = true;
        useBranchColumnLayout = false;
        showBranchHeaders = false;
        branchColumnSpacing = 300f;
        branchRowSpacing = 112f;
        branchSiblingSpacing = 86f;
        branchStartY = 160f;
        branchHeaderOffsetY = 92f;
        branchHeaderTextSize = 18f;
        useBranchLaneLayout = true;
        branchLaneSpacing = 112f;
        branchSameColumnSpacing = 62f;
        treeSwitcherSize = new Vector2(420f, 56f);
        treeSwitcherButtonSize = new Vector2(200f, 46f);
        treeSwitcherButtonSpacing = 20f;
        treeSwitcherTextSize = 20f;
        treeClipHorizontalPadding = 0f;
        treeClipTopPadding = 24f;
        treeClipBottomPadding = 24f;
        scrollContentPadding = new Vector2(160f, 160f);
        lineThickness = 5f;
        useMajorMinorNodeSizes = true;
        majorNodeSize = 76f;
        majorIconSize = 56f;
        minorNodeSize = 52f;
        minorIconSize = 34f;
        showNodeRankText = true;
        showNodeCostText = false;
        nodeRankTextSize = 8f;
        nodeRankTextRectSize = new Vector2(36f, 14f);
        nodeCostTextSize = 8f;
        nodeCostTextRectSize = new Vector2(24f, 14f);
        globalTextColor = Color.white;
        titleTextColor = new Color(0.9f, 0.96f, 1f, 1f);
        buttonTextColor = Color.white;
        nodeRankTextColor = Color.white;
        nodeCostTextColor = new Color(1f, 0.86f, 0.28f, 1f);
        resetButtonInset = new Vector2(28f, 54f);
        resetButtonSize = new Vector2(92f, 26f);
        resetButtonTextSize = 10f;
        resetButtonTopRight = false;
        defaultLineColor = new Color(0.98f, 0.84f, 0.36f, 0.82f);
        techniqueLineColor = new Color(0.95f, 0.76f, 0.32f, 0.82f);
        defenseLineColor = new Color(0.36f, 0.75f, 1f, 0.82f);
        controlLineColor = new Color(0.42f, 0.95f, 0.64f, 0.82f);
    }

    private static RectTransform CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, float fontSize = 12f)
    {
        RectTransform button = CreatePanel(parent, name, null);
        SetCentered(button, position, size);
        Image image = button.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        button.gameObject.AddComponent<Button>();
        TMP_Text text = CreateText(button, "Text", label, fontSize, TextAlignmentOptions.Center, Vector2.zero, size);
        text.color = activeButtonTextColor;
        return button;
    }

    private static RectTransform CreateCircleMask(Transform parent, string name, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateEmpty(parent, name);
        SetCentered(rect, position, size);
        rect.gameObject.AddComponent<CanvasRenderer>();

        UICircleGraphic circle = rect.gameObject.AddComponent<UICircleGraphic>();
        circle.color = Color.white;
        circle.raycastTarget = false;

        Mask mask = rect.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        return rect;
    }

    private static RectTransform CreateCircularIcon(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, bool raycastTarget)
    {
        RectTransform root = CreateEmpty(parent, name);
        SetCentered(root, position, size);
        CreateCircleGraphic(root, "IconBack", Vector2.zero, size, new Color(0.02f, 0.04f, 0.08f, 0.92f));

        RectTransform mask = CreateCircleMask(root, "IconMask", Vector2.zero, size);
        RectTransform icon = CreateImage(mask, "Icon", sprite, Vector2.zero, size);
        Image image = icon.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = raycastTarget;

        return root;
    }

    private static RectTransform CreateCircleGraphic(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateEmpty(parent, name);
        SetCentered(rect, position, size);
        rect.gameObject.AddComponent<CanvasRenderer>();

        UICircleGraphic circle = rect.gameObject.AddComponent<UICircleGraphic>();
        circle.color = color;
        circle.raycastTarget = false;
        return rect;
    }

    private static void RepairMissingCanvasRenderers(Transform root)
    {
        if (root == null) return;

        foreach (MaskableGraphic graphic in root.GetComponentsInChildren<MaskableGraphic>(true))
        {
            if (graphic != null && graphic.GetComponent<CanvasRenderer>() == null)
                graphic.gameObject.AddComponent<CanvasRenderer>();
        }

        foreach (Mask mask in root.GetComponentsInChildren<Mask>(true))
        {
            if (mask != null && mask.GetComponent<CanvasRenderer>() == null)
                mask.gameObject.AddComponent<CanvasRenderer>();
        }
    }

    private static RectTransform CreatePanel(Transform parent, string name, Sprite sprite)
    {
        RectTransform rect = CreateEmpty(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false;
        return rect;
    }

    private static void AddRaycastSurface(RectTransform rect)
    {
        if (rect == null)
            return;

        Image image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();

        image.sprite = null;
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;
    }

    private static void AddViewportMask(RectTransform rect, float horizontalPadding, float topPadding, float bottomPadding)
    {
        if (rect == null)
            return;

        Image image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();

        image.sprite = null;
        image.color = Color.white;
        image.raycastTarget = true;
        image.maskable = true;

        Mask mask = rect.GetComponent<Mask>();
        if (mask == null)
            mask = rect.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectMask2D rectMask = rect.GetComponent<RectMask2D>();
        if (rectMask == null)
            rectMask = rect.gameObject.AddComponent<RectMask2D>();

        rectMask.padding = new Vector4(horizontalPadding, bottomPadding, horizontalPadding, topPadding);
    }

    private static RectTransform CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(parent, name, sprite);
        SetCentered(rect, position, size);

        Image image = rect.GetComponent<Image>();
        image.preserveAspect = true;
        return rect;
    }

    private static Vector2 Scale(Vector2 value, float scale)
    {
        return value * scale;
    }

    private static Vector2 Max(Vector2 value, Vector2 min)
    {
        return new Vector2(Mathf.Max(value.x, min.x), Mathf.Max(value.y, min.y));
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Vector2 position, Vector2 rectSize)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        SetCentered(rect, position, rectSize);

        TMP_Text label = go.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        if (activeTextFont != null)
            label.font = activeTextFont;
        label.color = activeTextColor;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateEmpty(Transform parent, string name)
    {
        GameObject go = new(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void ApplySprite(RectTransform rect, Sprite sprite)
    {
        Image image = rect != null ? rect.GetComponent<Image>() : null;
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
        image.color = Color.white;
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void AnchorTopRight(RectTransform rect, Vector2 inset, Vector2 size)
    {
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-inset.x, -inset.y);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void AnchorTopLeft(RectTransform rect, Vector2 inset, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(inset.x, -inset.y);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        Anchor(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private static bool IsZero(Vector2 value)
    {
        return Mathf.Approximately(value.x, 0f) && Mathf.Approximately(value.y, 0f);
    }

    private static string GetShortLabel(SkillTreeNodeDefinition node)
    {
        string source = string.IsNullOrWhiteSpace(node.DisplayName) ? node.name : node.DisplayName;
        string[] words = source.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return "SK";

        string label = string.Empty;
        foreach (string word in words)
        {
            label += char.ToUpperInvariant(word[0]);
            if (label.Length == 4)
                break;
        }

        return label;
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Skill";

        return ObjectNames.NicifyVariableName(value).Replace(" ", string.Empty);
    }

    private static string SanitizeTreeContentName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Tree";

        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                chars[i] = '_';
        }

        return new string(chars);
    }

    private static Sprite LoadSprite(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
