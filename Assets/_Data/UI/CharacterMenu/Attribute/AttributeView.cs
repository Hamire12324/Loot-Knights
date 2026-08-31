using System.Collections.Generic;
using UnityEngine;

public class AttributeView : BaseMonoBehaviour
{
    [Header("Source")]
    [SerializeField] private CharacterStat characterStat;
    [SerializeField] private PlayerEquipmentManager equipmentManager;
    [SerializeField] private bool useLocalPlayerStats = true;
    [SerializeField] private CharacterClassAttributeData[] classAttributes;

    [Header("Texts")]
    [SerializeField] private AttributeText[] attributeTexts;
    [SerializeField] private string emptyValue = "-";
    private PlayerSkillTreeManager skillTreeManager;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        attributeTexts ??= GetComponentsInChildren<AttributeText>(true);
        equipmentManager ??= PlayerEquipmentManager.InstanceOrNull
            ?? FindAnyObjectByType<PlayerEquipmentManager>(FindObjectsInactive.Include);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Refresh();
        SubscribeEvents();
    }

    protected override void OnDisable()
    {
        UnsubscribeEvents();
        base.OnDisable();
    }

    public void Refresh()
    {
        SetCharacterStat(FindCharacterStat());

        CharacterAttributeData data = characterStat != null
            ? CharacterStatService.FromCharacterStat(characterStat)
            : CharacterStatService.FromProfile(classAttributes, equipmentManager, GetSkillTrees());

        foreach (AttributeText attributeText in attributeTexts)
            attributeText?.Refresh(data, emptyValue);
    }

    private CharacterStat FindCharacterStat()
    {
        if (!useLocalPlayerStats)
            return characterStat;

        HeroCtrl hero = HeroCtrl.GetLocal()
            ?? FindAnyObjectByType<HeroCtrl>(FindObjectsInactive.Exclude);

        if (hero == null)
        {
            HeroGameplaySpawner spawner = FindAnyObjectByType<HeroGameplaySpawner>(FindObjectsInactive.Include);
            hero = spawner != null ? spawner.SpawnedHero : null;
        }

        return hero != null ? hero.CharacterStat : characterStat;
    }

    private void SetCharacterStat(CharacterStat newCharacterStat)
    {
        if (characterStat == newCharacterStat) return;

        UnsubscribeStatEvents();
        characterStat = newCharacterStat;
        SubscribeStatEvents();
    }

    private IReadOnlyList<SkillTreeDefinition> GetSkillTrees()
    {
        CharacterMenuPanel menu = GetComponentInParent<CharacterMenuPanel>();
        if (menu != null)
            return menu.GetSkillTreesForCurrentProfile();

        SkillTreeView skillTree = GetComponentInParent<SkillTreeView>();
        return skillTree != null ? skillTree.GetSkillTrees() : System.Array.Empty<SkillTreeDefinition>();
    }

    private void SubscribeEvents()
    {
        SubscribeStatEvents();

        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentChanged -= Refresh;
            equipmentManager.OnEquipmentChanged += Refresh;
        }

        skillTreeManager = PlayerSkillTreeManager.Service;
        if (skillTreeManager != null)
        {
            skillTreeManager.OnChanged -= Refresh;
            skillTreeManager.OnChanged += Refresh;
        }
        PlayerAttributePointStorage.OnPointsChanged -= Refresh;
        PlayerAttributePointStorage.OnPointsChanged += Refresh;
    }

    private void UnsubscribeEvents()
    {
        UnsubscribeStatEvents();

        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged -= Refresh;

        if (skillTreeManager != null)
            skillTreeManager.OnChanged -= Refresh;

        skillTreeManager = null;
        PlayerAttributePointStorage.OnPointsChanged -= Refresh;
    }

    private void SubscribeStatEvents()
    {
        if (characterStat == null) return;

        characterStat.OnHealthChanged -= OnHealthChanged;
        characterStat.OnHealthChanged += OnHealthChanged;
        characterStat.OnStatChanged -= OnStatChanged;
        characterStat.OnStatChanged += OnStatChanged;
    }

    private void UnsubscribeStatEvents()
    {
        if (characterStat == null) return;

        characterStat.OnHealthChanged -= OnHealthChanged;
        characterStat.OnStatChanged -= OnStatChanged;
    }

    private void OnHealthChanged(float _) => Refresh();
    private void OnStatChanged(StatType _) => Refresh();
}
