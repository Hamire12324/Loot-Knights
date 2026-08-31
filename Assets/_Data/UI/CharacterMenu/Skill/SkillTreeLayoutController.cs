using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTreeLayoutController
{
    private const string TreeContentPrefix = "TreeContent_";

    private readonly Transform root;
    private readonly Dictionary<SkillTreeDefinition, SkillTreeNodeView[]> nodeViewsByTree = new();
    private ScrollRect treeScrollRect;

    public SkillTreeLayoutController(Transform root)
    {
        this.root = root;
    }

    public void ApplyVisibility(SkillTreeDefinition activeTree, SkillTreeDefinition primaryTree)
    {
        Transform content = Find("TreeArea/Viewport/Content");
        if (content == null)
            return;

        bool hasTreeContent = false;
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child == null || !child.name.StartsWith(TreeContentPrefix, StringComparison.Ordinal))
                continue;

            hasTreeContent = true;
            child.gameObject.SetActive(IsTreeContentFor(child, activeTree));
        }

        if (!hasTreeContent)
        {
            Transform legacyLines = content.Find("Lines");
            if (legacyLines != null)
                legacyLines.gameObject.SetActive(activeTree == primaryTree);
        }

        ConfigureScroll(activeTree);
    }

    public void ResetScroll(SkillTreeDefinition activeTree)
    {
        treeScrollRect ??= Find("TreeArea")?.GetComponent<ScrollRect>();
        if (treeScrollRect == null)
            return;

        ConfigureScroll(activeTree);
        Canvas.ForceUpdateCanvases();
        treeScrollRect.horizontalNormalizedPosition = 0.5f;
        treeScrollRect.verticalNormalizedPosition = 1f;
    }

    public Transform GetTreeContentRoot(SkillTreeDefinition tree)
    {
        Transform content = Find("TreeArea/Viewport/Content");
        if (content == null || tree == null)
            return null;

        return content.Find(GetTreeContentName(tree));
    }

    public SkillTreeNodeView[] GetNodeViews(SkillTreeDefinition tree)
    {
        Transform treeRoot = GetTreeContentRoot(tree);
        if (treeRoot == null || tree == null)
            return Array.Empty<SkillTreeNodeView>();

        if (nodeViewsByTree.TryGetValue(tree, out SkillTreeNodeView[] cachedViews))
            return cachedViews;

        cachedViews = treeRoot.GetComponentsInChildren<SkillTreeNodeView>(true);
        nodeViewsByTree[tree] = cachedViews;
        return cachedViews;
    }

    public static bool IsElementalSkillTree(SkillTreeDefinition tree)
    {
        if (tree == null)
            return false;

        if (!string.IsNullOrWhiteSpace(tree.TreeId) &&
            tree.TreeId.IndexOf("element", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        foreach (SkillTreeNodeDefinition node in tree.Nodes)
        {
            if (node != null &&
                (node.Element != ElementType.None ||
                 node.Reaction != ElementalReactionType.None ||
                 node.Kind == SkillTreeNodeKind.ElementUnlock ||
                 node.Kind == SkillTreeNodeKind.ElementReaction))
            {
                return true;
            }
        }

        return false;
    }

    private void ConfigureScroll(SkillTreeDefinition activeTree)
    {
        treeScrollRect ??= Find("TreeArea")?.GetComponent<ScrollRect>();
        if (treeScrollRect == null)
            return;

        RectTransform scrollContent = Find("TreeArea/Viewport/Content") as RectTransform;
        if (scrollContent != null && treeScrollRect.content != scrollContent)
            treeScrollRect.content = scrollContent;

        RectTransform viewport = treeScrollRect.viewport != null
            ? treeScrollRect.viewport
            : Find("TreeArea/Viewport") as RectTransform;

        if (scrollContent == null || viewport == null)
            return;

        treeScrollRect.horizontal = false;
        treeScrollRect.horizontalNormalizedPosition = 0.5f;
        treeScrollRect.vertical = NeedsVerticalScroll(activeTree, viewport);

        if (!treeScrollRect.vertical)
            treeScrollRect.velocity = Vector2.zero;
    }

    private bool NeedsVerticalScroll(SkillTreeDefinition tree, RectTransform viewport)
    {
        SkillTreeNodeView[] nodeViews = GetNodeViews(tree);
        if (nodeViews.Length == 0)
            return false;

        bool hasBounds = false;
        float minY = 0f;
        float maxY = 0f;
        Vector3[] corners = new Vector3[4];

        foreach (SkillTreeNodeView nodeView in nodeViews)
        {
            if (nodeView == null || nodeView.transform is not RectTransform rect)
                continue;

            rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                float y = viewport.InverseTransformPoint(corner).y;
                if (!hasBounds)
                {
                    minY = maxY = y;
                    hasBounds = true;
                }
                else
                {
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }

        return hasBounds && maxY - minY > viewport.rect.height - 24f;
    }

    private Transform Find(string path)
    {
        if (root == null)
            return null;

        Transform result = root.Find(path);
        if (result != null || !path.StartsWith("TreeArea/", StringComparison.Ordinal))
            return result;

        SkillTreeTreeAreaView treeAreaView = root.GetComponentInChildren<SkillTreeTreeAreaView>(true);
        return treeAreaView != null
            ? treeAreaView.transform.Find(path.Substring("TreeArea/".Length))
            : null;
    }

    private static bool IsTreeContentFor(Transform contentRoot, SkillTreeDefinition tree)
    {
        return contentRoot != null && tree != null && contentRoot.name == GetTreeContentName(tree);
    }

    private static string GetTreeContentName(SkillTreeDefinition tree)
    {
        string rawName = tree != null ? tree.TreeId : "Tree";
        return TreeContentPrefix + SanitizeName(rawName);
    }

    private static string SanitizeName(string value)
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
}
