#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EnemySkillAssetInstaller
{
    private const string EffectsRootPath = "Assets/_Data/Characters/Shared/Skill/Effects/Enemy";
    private const string CommonEffectsPath = EffectsRootPath + "/Common";
    private const string PoisonSlimeEffectsPath = EffectsRootPath + "/PoisonSlime";
    private const string DefinitionsPath = "Assets/_Data/Characters/Enemy/Skill/Definitions/Base Attacks";

    [MenuItem("Tools/Loot Knights/Rebuild Enemy Skill Assets")]
    public static void Rebuild()
    {
        EnsureFolder("Assets/_Data/Characters/Shared/Skill/Effects", "Enemy");
        EnsureFolder(EffectsRootPath, "Common");
        EnsureFolder(EffectsRootPath, "PoisonSlime");
        EnsureFolder("Assets/_Data/Characters/Enemy/Skill/Definitions", "Base Attacks");

        CharacterSkillBasicAttackEffect hitboxEffect = GetOrCreate<CharacterSkillBasicAttackEffect>(CommonEffectsPath + "/EnemyHitboxAttackEffect.asset");
        Set(hitboxEffect, "dealHitboxDamageAtCastTime", true);

        CharacterSkillRepeatingAreaDamageEffect poisonArea = GetOrCreate<CharacterSkillRepeatingAreaDamageEffect>(PoisonSlimeEffectsPath + "/PoisonSlimeAreaEffect.asset");
        ConfigurePoisonArea(poisonArea);

        EnemySkillDefinition melee = GetOrCreate<EnemySkillDefinition>(DefinitionsPath + "/EnemyMeleeBasicAttack.asset");
        ConfigureSkill(melee, "enemy_melee_basic", "Enemy Melee Basic Attack", 0.18f, 0.9f, hitboxEffect);

        EnemySkillDefinition bat = GetOrCreate<EnemySkillDefinition>(DefinitionsPath + "/BatBiteBasicAttack.asset");
        ConfigureSkill(bat, "bat_bite_basic", "Bat Bite", 0.08f, 1.1f, hitboxEffect);

        EnemySkillDefinition poison = GetOrCreate<EnemySkillDefinition>(DefinitionsPath + "/PoisonSlimeBasicAttack.asset");
        ConfigureSkill(poison, "poison_slime_basic", "Poison Slime Attack", 0.25f, 1.25f, hitboxEffect, poisonArea);

        VFXDefinition arrowVfx = GetOrCreate<VFXDefinition>("Assets/_Data/Core/VFX/EnemyArrowProjectileVfx.asset");
        arrowVfx.Prefab = AssetDatabase.LoadAssetAtPath<PoolObj>("Assets/_Data/Characters/Hero/Animation/Archer/Arrow01.prefab");
        arrowVfx.Scale = 1f;
        EditorUtility.SetDirty(arrowVfx);

        CharacterSkillProjectileEffect arrowEffect = GetOrCreate<CharacterSkillProjectileEffect>(CommonEffectsPath + "/EnemyArrowProjectileEffect.asset");
        ConfigureArrowProjectile(arrowEffect, arrowVfx);

        EnemySkillDefinition arrowShot = GetOrCreate<EnemySkillDefinition>(DefinitionsPath + "/EnemyArrowShot.asset");
        ConfigureSkill(arrowShot, "enemy_arrow_shot", "Enemy Arrow Shot", 0.2f, 1.2f, arrowEffect);

        AssignBasicAttack("Assets/_Data/Characters/Enemy/Prefabs/Done/BatCtrl.prefab", bat);
        AssignBasicAttack("Assets/_Data/Characters/Enemy/Prefabs/Done/PoisonSlimeCtrl.prefab", poison);
        AssignBasicAttack("Assets/_Data/Characters/Enemy/Prefabs/Done/SkeletonArcherCtrl.prefab", arrowShot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Enemy skill assets rebuilt.");
    }

    private static void ConfigurePoisonArea(CharacterSkillRepeatingAreaDamageEffect effect)
    {
        SerializedObject serialized = new(effect);
        serialized.FindProperty("radius").floatValue = 1.25f;
        serialized.FindProperty("forwardOffset").floatValue = 0.45f;
        serialized.FindProperty("duration").floatValue = 3f;
        serialized.FindProperty("tickInterval").floatValue = 0.5f;
        serialized.FindProperty("targetLayer").intValue = LayerMask.GetMask("Player");
        serialized.FindProperty("areaVfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VFXDefinition>("Assets/Resources/VFX/Enemy/VFX_Poison.asset");
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSkill(EnemySkillDefinition skill, string id, string displayName, float castTime, float cooldown, params CharacterSkillEffectDefinition[] effects)
    {
        SerializedObject serialized = new(skill);
        serialized.FindProperty("skillId").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("castType").enumValueIndex = (int)CharacterSkillCastType.CastTime;
        serialized.FindProperty("castTime").floatValue = castTime;
        serialized.FindProperty("cooldown").floatValue = cooldown;
        SerializedProperty list = serialized.FindProperty("effects");
        list.arraySize = effects.Length;
        for (int i = 0; i < effects.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = effects[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureArrowProjectile(CharacterSkillProjectileEffect effect, VFXDefinition vfx)
    {
        SerializedObject serialized = new(effect);
        serialized.FindProperty("projectileVfx").objectReferenceValue = vfx;
        serialized.FindProperty("length").floatValue = 8f;
        serialized.FindProperty("startOffset").floatValue = 0.45f;
        serialized.FindProperty("speed").floatValue = 11f;
        serialized.FindProperty("targetLayer").intValue = LayerMask.GetMask("Player");
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignBasicAttack(string prefabPath, EnemySkillDefinition skill)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        EnemySkillController controller = root.GetComponentInChildren<EnemySkillController>(true);
        if (controller != null)
            Set(controller, "basicAttack", skill);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void Set(Object target, string property, object value)
    {
        SerializedObject serialized = new(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (value is bool boolValue) field.boolValue = boolValue;
        else field.objectReferenceValue = value as Object;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
