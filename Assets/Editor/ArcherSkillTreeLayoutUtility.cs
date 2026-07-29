using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ArcherSkillTreeLayoutUtility
{
    private const string TreePath = "Assets/_Data/SkillTrees/Archer/Archer_SkillTree.asset";
    private const string NodeDir = "Assets/_Data/SkillTrees/Archer/";

    [MenuItem("Loot Knights/Skill Trees/Apply Knight Layout To Archer")]
    public static void ApplyKnightLayoutToArcher()
    {
        Dictionary<string, Vector2> positions = new()
        {
            ["Node_ArcherPath.asset"] = new Vector2(0f, 300f),
            ["Node_PiercingShot.asset"] = new Vector2(-390f, 170f),
            ["Node_Longshot.asset"] = new Vector2(-470f, 50f),
            ["Node_BarbedArrow.asset"] = new Vector2(-310f, 50f),
            ["Node_QuickDraw.asset"] = new Vector2(-350f, -70f),
            ["Node_SplitShot.asset"] = new Vector2(-270f, -70f),
            ["Node_Ricochet.asset"] = new Vector2(-350f, -190f),
            ["Node_VongLapVoTan.asset"] = new Vector2(-270f, -190f),
            ["Node_SanDuoiKhongLoiThoat.asset"] = new Vector2(-350f, -310f),

            ["Node_RainOfArrows.asset"] = new Vector2(-130f, 170f),
            ["Node_Bullseye.asset"] = new Vector2(-210f, 50f),
            ["Node_SteadyAim.asset"] = new Vector2(-50f, 50f),
            ["Node_PowerShot.asset"] = new Vector2(-90f, -70f),
            ["Node_Skirmisher.asset"] = new Vector2(-10f, -70f),
            ["Node_Deadeye.asset"] = new Vector2(-90f, -190f),
            ["Node_TuTu.asset"] = new Vector2(-10f, -190f),
            ["Node_MeCungTienThuat.asset"] = new Vector2(-90f, -310f),

            ["Node_TrickArrow.asset"] = new Vector2(130f, 170f),
            ["Node_EvasiveRoll.asset"] = new Vector2(50f, 50f),
            ["Node_LightStep.asset"] = new Vector2(210f, 50f),
            ["Node_WindRunner.asset"] = new Vector2(170f, -70f),
            ["Node_SurvivalInstinct.asset"] = new Vector2(250f, -70f),
            ["Node_FleetFooting.asset"] = new Vector2(170f, -190f),
            ["Node_KeTheMang.asset"] = new Vector2(250f, -190f),
            ["Node_DuongBayTuThan.asset"] = new Vector2(170f, -310f),

            ["Node_BasicShot.asset"] = new Vector2(390f, 170f),
            ["Node_DrawTraining.asset"] = new Vector2(310f, 50f),
            ["Node_KeenEye.asset"] = new Vector2(470f, 50f),
            ["Node_HawkFocus.asset"] = new Vector2(270f, -70f),
            ["Node_VitalSpot.asset"] = new Vector2(350f, -70f),
            ["Node_PiercingMastery.asset"] = new Vector2(470f, -70f),
            ["Node_CuocSanKhongKetThuc.asset"] = new Vector2(350f, -190f),
            ["Node_XoaTenKhoiCuocSan.asset"] = new Vector2(470f, -190f),
            ["Node_ApexHunter.asset"] = new Vector2(0f, -450f),
        };

        foreach (KeyValuePair<string, Vector2> pair in positions)
        {
            SkillTreeNodeDefinition node = AssetDatabase.LoadAssetAtPath<SkillTreeNodeDefinition>(NodeDir + pair.Key);
            if (node == null)
            {
                Debug.LogWarning($"Missing Archer skill tree node: {pair.Key}");
                continue;
            }

            SerializedObject nodeSo = new(node);
            nodeSo.FindProperty("treePosition").vector2Value = pair.Value;
            nodeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(node);
        }

        ApplyPrerequisites();

        SkillTreeDefinition tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(TreePath);
        if (tree != null)
        {
            SerializedObject treeSo = new(tree);
            SerializedProperty settings = treeSo.FindProperty("viewSettings");
            settings.FindPropertyRelative("overrideBuilderSettings").boolValue = false;
            settings.FindPropertyRelative("contentSize").vector2Value = new Vector2(1100f, 860f);
            settings.FindPropertyRelative("contentOffset").vector2Value = Vector2.zero;
            settings.FindPropertyRelative("contentScale").floatValue = 1f;
            settings.FindPropertyRelative("contentScale2D").vector2Value = Vector2.one;
            settings.FindPropertyRelative("nodeSize").floatValue = 86f;
            settings.FindPropertyRelative("iconSize").floatValue = 64f;
            settings.FindPropertyRelative("nodeIconFramePadding").floatValue = 24f;
            settings.FindPropertyRelative("useMajorMinorNodeSizes").boolValue = true;
            settings.FindPropertyRelative("majorNodeSize").floatValue = 76f;
            settings.FindPropertyRelative("majorIconSize").floatValue = 56f;
            settings.FindPropertyRelative("minorNodeSize").floatValue = 52f;
            settings.FindPropertyRelative("minorIconSize").floatValue = 34f;
            settings.FindPropertyRelative("autoExpandScrollContent").boolValue = true;
            settings.FindPropertyRelative("scrollContentPadding").vector2Value = new Vector2(160f, 160f);
            settings.FindPropertyRelative("horizontalScroll").boolValue = false;
            settings.FindPropertyRelative("startTreeAtTop").boolValue = true;
            treeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tree);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied Knight-style layout to Archer skill tree.");
    }

    private static void ApplyPrerequisites()
    {
        SetPrerequisites("Node_ArcherPath.asset");

        SetPrerequisites("Node_PiercingShot.asset", "Node_ArcherPath.asset");
        SetPrerequisites("Node_RainOfArrows.asset", "Node_ArcherPath.asset");
        SetPrerequisites("Node_TrickArrow.asset", "Node_ArcherPath.asset");
        SetPrerequisites("Node_BasicShot.asset", "Node_ArcherPath.asset");

        SetPrerequisites("Node_Longshot.asset", "Node_PiercingShot.asset");
        SetPrerequisites("Node_BarbedArrow.asset", "Node_PiercingShot.asset");
        SetPrerequisites("Node_QuickDraw.asset", "Node_BarbedArrow.asset");
        SetPrerequisites("Node_SplitShot.asset", "Node_BarbedArrow.asset");
        SetPrerequisites("Node_Ricochet.asset", "Node_QuickDraw.asset");
        SetPrerequisites("Node_VongLapVoTan.asset", "Node_SplitShot.asset");
        SetPrerequisites("Node_SanDuoiKhongLoiThoat.asset", "Node_Ricochet.asset");

        SetPrerequisites("Node_Bullseye.asset", "Node_RainOfArrows.asset");
        SetPrerequisites("Node_SteadyAim.asset", "Node_RainOfArrows.asset");
        SetPrerequisites("Node_PowerShot.asset", "Node_SteadyAim.asset");
        SetPrerequisites("Node_Skirmisher.asset", "Node_SteadyAim.asset");
        SetPrerequisites("Node_Deadeye.asset", "Node_PowerShot.asset");
        SetPrerequisites("Node_TuTu.asset", "Node_Skirmisher.asset");
        SetPrerequisites("Node_MeCungTienThuat.asset", "Node_Deadeye.asset");

        SetPrerequisites("Node_EvasiveRoll.asset", "Node_TrickArrow.asset");
        SetPrerequisites("Node_LightStep.asset", "Node_TrickArrow.asset");
        SetPrerequisites("Node_WindRunner.asset", "Node_LightStep.asset");
        SetPrerequisites("Node_SurvivalInstinct.asset", "Node_LightStep.asset");
        SetPrerequisites("Node_FleetFooting.asset", "Node_WindRunner.asset");
        SetPrerequisites("Node_KeTheMang.asset", "Node_SurvivalInstinct.asset");
        SetPrerequisites("Node_DuongBayTuThan.asset", "Node_FleetFooting.asset");

        SetPrerequisites("Node_DrawTraining.asset", "Node_BasicShot.asset");
        SetPrerequisites("Node_KeenEye.asset", "Node_BasicShot.asset");
        SetPrerequisites("Node_HawkFocus.asset", "Node_DrawTraining.asset");
        SetPrerequisites("Node_VitalSpot.asset", "Node_DrawTraining.asset");
        SetPrerequisites("Node_PiercingMastery.asset", "Node_KeenEye.asset");
        SetPrerequisites("Node_CuocSanKhongKetThuc.asset", "Node_VitalSpot.asset");
        SetPrerequisites("Node_XoaTenKhoiCuocSan.asset", "Node_PiercingMastery.asset");
        SetPrerequisites(
            "Node_ApexHunter.asset",
            "Node_SanDuoiKhongLoiThoat.asset",
            "Node_MeCungTienThuat.asset",
            "Node_DuongBayTuThan.asset",
            "Node_XoaTenKhoiCuocSan.asset");
    }

    private static void SetPrerequisites(string nodeAssetName, params string[] prerequisiteAssetNames)
    {
        SkillTreeNodeDefinition node = AssetDatabase.LoadAssetAtPath<SkillTreeNodeDefinition>(NodeDir + nodeAssetName);
        if (node == null)
            return;

        SerializedObject nodeSo = new(node);
        SerializedProperty prerequisites = nodeSo.FindProperty("prerequisites");
        prerequisites.arraySize = prerequisiteAssetNames.Length;

        for (int i = 0; i < prerequisiteAssetNames.Length; i++)
        {
            SkillTreeNodeDefinition prerequisite = AssetDatabase.LoadAssetAtPath<SkillTreeNodeDefinition>(NodeDir + prerequisiteAssetNames[i]);
            SerializedProperty item = prerequisites.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("node").objectReferenceValue = prerequisite;
            item.FindPropertyRelative("requiredRank").intValue = 1;
        }

        nodeSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(node);
    }
}
