#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-click setup for the Orc Rider's equipped combat skills and their animator bindings.
/// It intentionally composes the shared hitbox, dash and area-damage effects.
/// </summary>
public static class OrcRiderCombatSetupUtility
{
    private const string PrefabPath = "Assets/_Data/Characters/Enemy/Prefabs/Orc/OrcRiderCtrl.prefab";
    private const string AnimatorPath = "Assets/_Data/Characters/Enemy/Animation/Orc/OrcRider/OrcRider.controller";
    private const string EffectsRootPath = "Assets/_Data/Characters/Shared/Skill/Effects/Enemy";
    private const string CommonEffectsPath = EffectsRootPath + "/Common";
    private const string OrcRiderEffectsPath = EffectsRootPath + "/OrcRider";
    private const string BeastbreakerEffectsPath = OrcRiderEffectsPath + "/Beastbreaker";
    private const string MaelstromEffectsPath = OrcRiderEffectsPath + "/Maelstrom";
    private const string DefinitionsPath = "Assets/_Data/Characters/Enemy/Skill/Definitions/OrcRider";

    [MenuItem("Tools/Loot Knights/Configure Orc Rider Combat")]
    public static void Configure()
    {
        EnsureFolder("Assets/_Data/Characters/Enemy/Skill/Definitions", "OrcRider");
        EnsureFolder(EffectsRootPath, "Common");
        EnsureFolder(EffectsRootPath, "OrcRider");
        EnsureFolder(OrcRiderEffectsPath, "Beastbreaker");
        EnsureFolder(OrcRiderEffectsPath, "Maelstrom");

        CharacterSkillBasicAttackEffect hitbox = AssetDatabase.LoadAssetAtPath<CharacterSkillBasicAttackEffect>(
            CommonEffectsPath + "/EnemyHitboxAttackEffect.asset");
        if (hitbox == null)
        {
            Debug.LogError("Orc Rider setup needs EnemyHitboxAttackEffect. Run 'Rebuild Enemy Skill Assets' first.");
            return;
        }

        CharacterSkillDashEffect chargeDash = GetOrCreate<CharacterSkillDashEffect>(BeastbreakerEffectsPath + "/OrcRider_BeastbreakerDash.asset");
        ConfigureDash(chargeDash);

        CharacterSkillAreaDamageEffect chargeImpact = GetOrCreate<CharacterSkillAreaDamageEffect>(BeastbreakerEffectsPath + "/OrcRider_BeastbreakerImpact.asset");
        ConfigureAreaDamage(chargeImpact, 0.95f, 110f, 0.55f, 0.4f, 1.35f, 0.18f);

        CharacterSkillRepeatingAreaDamageEffect maelstrom = GetOrCreate<CharacterSkillRepeatingAreaDamageEffect>(MaelstromEffectsPath + "/OrcRider_MaelstromTicks.asset");
        ConfigureRepeatingAreaDamage(maelstrom);

        EnemyBlockSkillDefinition guard = GetOrCreate<EnemyBlockSkillDefinition>(DefinitionsPath + "/OrcRider_IronhideGuard.asset");
        ConfigureSkill(guard, "orc_rider_ironhide_guard", "Ironhide Guard", CharacterSkillCastType.Duration, 0f, 0.7f, 3.5f, "Block");
        SetFloat(guard, "damageMultiplier", 0.35f);

        EnemySkillDefinition skullsplitter = GetOrCreate<EnemySkillDefinition>(DefinitionsPath + "/OrcRider_Skullsplitter.asset");
        ConfigureSkill(skullsplitter, "orc_rider_skullsplitter", "Skullsplitter", CharacterSkillCastType.CastTime, 0.22f, 0f, 1.15f, "Basic_Attack", hitbox);

        EnemySkillDefinition charge = GetOrCreate<EnemySkillDefinition>(DefinitionsPath + "/OrcRider_BeastbreakerCharge.asset");
        ConfigureSkill(charge, "orc_rider_beastbreaker_charge", "Beastbreaker Charge", CharacterSkillCastType.CastTime, 0.28f, 0f, 5.5f, "OrcRider_Charge", chargeDash, chargeImpact);

        EnemySkillDefinition spin = GetOrCreate<EnemySkillDefinition>(DefinitionsPath + "/OrcRider_MaelstromMace.asset");
        ConfigureSkill(spin, "orc_rider_maelstrom_mace", "Maelstrom Mace", CharacterSkillCastType.Duration, 0.08f, 0.82f, 6.5f, "OrcRider_Maelstrom", maelstrom);

        ConfigureAnimator();
        ConfigurePrefab(guard, skullsplitter, charge, spin);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Orc Rider combat configured: Ironhide Guard, Skullsplitter, Beastbreaker Charge, Maelstrom Mace.");
    }

    private static void ConfigurePrefab(
        CharacterSkillDefinition guard,
        CharacterSkillDefinition skullsplitter,
        CharacterSkillDefinition charge,
        CharacterSkillDefinition spin)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            Debug.LogError($"Orc Rider setup could not find its prefab at '{PrefabPath}'.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        EnemySkillController oldController = root.GetComponentInChildren<EnemySkillController>(true);
        if (oldController == null)
        {
            PrefabUtility.UnloadPrefabContents(root);
            Debug.LogError("Orc Rider prefab has no EnemySkillController.");
            return;
        }

        GameObject controllerObject = oldController.gameObject;
        OrcRiderSkillController riderController = oldController as OrcRiderSkillController;
        if (riderController == null)
        {
            Object.DestroyImmediate(oldController);
            riderController = controllerObject.AddComponent<OrcRiderSkillController>();
        }

        SerializedObject controller = new(riderController);
        controller.FindProperty("basicAttack").objectReferenceValue = skullsplitter;
        SerializedProperty equipped = controller.FindProperty("equippedSkills");
        equipped.arraySize = 4;
        equipped.GetArrayElementAtIndex(0).objectReferenceValue = guard;
        equipped.GetArrayElementAtIndex(1).objectReferenceValue = charge;
        equipped.GetArrayElementAtIndex(2).objectReferenceValue = spin;
        equipped.GetArrayElementAtIndex(3).objectReferenceValue = null;
        controller.ApplyModifiedPropertiesWithoutUndo();

        EnemyCtrl enemy = root.GetComponentInChildren<EnemyCtrl>(true);
        if (enemy != null)
        {
            SerializedObject enemySerialized = new(enemy);
            enemySerialized.FindProperty("characterSkillController").objectReferenceValue = riderController;
            enemySerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ConfigureAnimator()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath);
        if (controller == null)
            return;

        EnsureTrigger(controller, "Block");
        EnsureTrigger(controller, "OrcRider_Charge");
        EnsureTrigger(controller, "OrcRider_Maelstrom");

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState move = FindState(machine, "Move");
        AnimatorState block = FindState(machine, "OrcRider_Block", "ArmoredOrc_Block");
        AnimatorState charge = FindState(machine, "OrcRider_Charge", "ArmoredOrc_Thrust");
        AnimatorState spin = FindState(machine, "OrcRider_Maelstrom", "ArmoredOrc_Sweep");
        if (move == null || block == null || charge == null || spin == null)
        {
            Debug.LogError("Orc Rider animator is missing Move, Block, Charge, or Maelstrom state.");
            return;
        }

        block.name = "OrcRider_Block";
        charge.name = "OrcRider_Charge";
        spin.name = "OrcRider_Maelstrom";
        block.motion = AssetDatabase.LoadAssetAtPath<Motion>("Assets/_Data/Characters/Enemy/Animation/Orc/OrcRider/Block.anim");
        charge.motion = AssetDatabase.LoadAssetAtPath<Motion>("Assets/_Data/Characters/Enemy/Animation/Orc/OrcRider/Attack02.anim");
        spin.motion = AssetDatabase.LoadAssetAtPath<Motion>("Assets/_Data/Characters/Enemy/Animation/Orc/OrcRider/Attack03.anim");

        RemoveAnyStateTransitions(machine, block, charge, spin);
        EnsureTransition(move, block, "Block");
        EnsureTransition(move, charge, "OrcRider_Charge");
        EnsureTransition(move, spin, "OrcRider_Maelstrom");
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureDash(CharacterSkillDashEffect effect)
    {
        SerializedObject serialized = new(effect);
        serialized.FindProperty("distance").floatValue = 3.4f;
        serialized.FindProperty("duration").floatValue = 0.4f;
        serialized.FindProperty("invincibleDuringDash").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureAreaDamage(
        CharacterSkillAreaDamageEffect effect,
        float radius,
        float angle,
        float forwardOffset,
        float delay,
        float multiplier,
        float hitStunDuration)
    {
        SerializedObject serialized = new(effect);
        serialized.FindProperty("radius").floatValue = radius;
        serialized.FindProperty("angle").floatValue = angle;
        serialized.FindProperty("forwardOffset").floatValue = forwardOffset;
        serialized.FindProperty("delay").floatValue = delay;
        serialized.FindProperty("targetLayer").intValue = LayerMask.GetMask("Player");
        SerializedProperty damage = serialized.FindProperty("damageData");
        damage.FindPropertyRelative("Multiplier").floatValue = multiplier;
        damage.FindPropertyRelative("CanCrit").boolValue = false;
        damage.FindPropertyRelative("CausesHitStun").boolValue = true;
        damage.FindPropertyRelative("HitStunDuration").floatValue = hitStunDuration;
        damage.FindPropertyRelative("HitStunImmunityDuration").floatValue = 0.65f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureRepeatingAreaDamage(CharacterSkillRepeatingAreaDamageEffect effect)
    {
        SerializedObject serialized = new(effect);
        serialized.FindProperty("radius").floatValue = 1.35f;
        serialized.FindProperty("forwardOffset").floatValue = 0f;
        serialized.FindProperty("followCaster").boolValue = true;
        serialized.FindProperty("duration").floatValue = 0.82f;
        serialized.FindProperty("tickInterval").floatValue = 0.18f;
        serialized.FindProperty("targetLayer").intValue = LayerMask.GetMask("Player");
        SerializedProperty damage = serialized.FindProperty("damageData");
        damage.FindPropertyRelative("Multiplier").floatValue = 0.42f;
        damage.FindPropertyRelative("CanCrit").boolValue = false;
        damage.FindPropertyRelative("CausesHitStun").boolValue = true;
        damage.FindPropertyRelative("HitStunDuration").floatValue = 0.08f;
        damage.FindPropertyRelative("HitStunImmunityDuration").floatValue = 0.12f;
        damage.FindPropertyRelative("IgnoresHitStunImmunity").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSkill(
        CharacterSkillDefinition skill,
        string id,
        string displayName,
        CharacterSkillCastType castType,
        float castTime,
        float duration,
        float cooldown,
        string trigger,
        params CharacterSkillEffectDefinition[] effects)
    {
        SerializedObject serialized = new(skill);
        serialized.FindProperty("skillId").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("castType").enumValueIndex = (int)castType;
        serialized.FindProperty("castTime").floatValue = castTime;
        serialized.FindProperty("duration").floatValue = duration;
        serialized.FindProperty("cooldown").floatValue = cooldown;
        serialized.FindProperty("triggerName").stringValue = trigger;
        serialized.FindProperty("executeEffectsOnAnimationHit").boolValue = false;
        SerializedProperty list = serialized.FindProperty("effects");
        list.arraySize = effects.Length;
        for (int i = 0; i < effects.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = effects[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string property, float value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(property).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static void EnsureTrigger(AnimatorController controller, string name)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == name)
                return;
        controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
    }

    private static AnimatorState FindState(AnimatorStateMachine machine, params string[] names)
    {
        foreach (ChildAnimatorState child in machine.states)
            foreach (string name in names)
                if (child.state.name == name)
                    return child.state;
        return null;
    }

    private static void RemoveAnyStateTransitions(AnimatorStateMachine machine, params AnimatorState[] states)
    {
        for (int i = machine.anyStateTransitions.Length - 1; i >= 0; i--)
        {
            AnimatorStateTransition transition = machine.anyStateTransitions[i];
            foreach (AnimatorState state in states)
            {
                if (transition.destinationState != state)
                    continue;
                machine.RemoveAnyStateTransition(transition);
                break;
            }
        }
    }

    private static void EnsureTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        foreach (AnimatorStateTransition transition in from.transitions)
        {
            if (transition.destinationState != to)
                continue;
            foreach (AnimatorCondition condition in transition.conditions)
                if (condition.parameter == trigger)
                    return;
        }

        AnimatorStateTransition created = from.AddTransition(to);
        created.hasExitTime = false;
        created.duration = 0f;
        created.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }
}
#endif
