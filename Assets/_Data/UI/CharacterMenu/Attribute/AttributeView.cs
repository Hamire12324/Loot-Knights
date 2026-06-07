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

    [Header("Display")]
    [SerializeField] private string emptyValue = "-";

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeStatEvents();
        SubscribeEquipmentEvents();
        Refresh();
    }

    protected override void OnDisable()
    {
        UnsubscribeStatEvents();
        UnsubscribeEquipmentEvents();
        base.OnDisable();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadCharacterStat();
        LoadEquipmentManager();
        LoadAttributeTexts();
    }

    public void Refresh()
    {
        CharacterStat resolvedStat = ResolveCharacterStat();
        if (resolvedStat != characterStat)
            SetCharacterStat(resolvedStat);

        AttributeStatSnapshot statSnapshot = characterStat != null
            ? AttributeStatSnapshot.FromCharacterStat(characterStat)
            : ResolveProfileStatSnapshot();

        if (attributeTexts == null || attributeTexts.Length == 0)
            LoadAttributeTexts();

        foreach (AttributeText attributeText in attributeTexts)
        {
            if (attributeText == null) continue;

            attributeText.Refresh(statSnapshot, emptyValue);
        }
    }

    private void LoadCharacterStat()
    {
        if (characterStat != null) return;

        SetCharacterStat(ResolveCharacterStat());
    }

    private CharacterStat ResolveCharacterStat()
    {
        if (!useLocalPlayerStats && characterStat != null)
            return characterStat;

        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero != null && hero.CharacterStat != null)
            return hero.CharacterStat;

        HeroGameplaySpawner spawner = FindAnyObjectByType<HeroGameplaySpawner>(FindObjectsInactive.Include);
        if (spawner != null && spawner.SpawnedHero != null && spawner.SpawnedHero.CharacterStat != null)
            return spawner.SpawnedHero.CharacterStat;

        HeroCtrl sceneHero = FindAnyObjectByType<HeroCtrl>(FindObjectsInactive.Exclude);
        if (sceneHero != null && sceneHero.CharacterStat != null)
            return sceneHero.CharacterStat;

        return characterStat;
    }

    private AttributeStatSnapshot ResolveProfileStatSnapshot()
    {
        CreatedCharacterData character = CharacterProfileStorage.Load();
        CharacterClass characterClass = character != null
            ? character.CharacterClass
            : CharacterClass.Knight;

        if (classAttributes != null)
        {
            foreach (CharacterClassAttributeData classAttribute in classAttributes)
            {
                if (classAttribute == null || classAttribute.CharacterClass != characterClass) continue;

                return ApplyEquipment(classAttribute.ToSnapshot());
            }
        }

        return ApplyEquipment(GetDefaultSnapshot(characterClass));
    }

    private AttributeStatSnapshot GetDefaultSnapshot(CharacterClass characterClass)
    {
        return characterClass switch
        {
            CharacterClass.Ranger => new AttributeStatSnapshot(90f, 0f, 90f, 90f, 0.1f, 1.5f),
            CharacterClass.Mage => new AttributeStatSnapshot(120f, 0f, 80f, 80f, 0.05f, 1.7f),
            _ => new AttributeStatSnapshot(100f, 0f, 100f, 100f, 0.05f, 1.5f)
        };
    }

    private void SetCharacterStat(CharacterStat newCharacterStat)
    {
        if (characterStat == newCharacterStat) return;

        UnsubscribeStatEvents();
        characterStat = newCharacterStat;
        SubscribeStatEvents();
    }

    private void LoadAttributeTexts()
    {
        if (attributeTexts != null && attributeTexts.Length > 0) return;

        attributeTexts = GetComponentsInChildren<AttributeText>(true);
    }

    private void LoadEquipmentManager()
    {
        if (equipmentManager != null) return;

        if (PlayerEquipmentManager.InstanceOrNull != null)
        {
            equipmentManager = PlayerEquipmentManager.InstanceOrNull;
            return;
        }

        equipmentManager = FindAnyObjectByType<PlayerEquipmentManager>(FindObjectsInactive.Include);
    }

    private AttributeStatSnapshot ApplyEquipment(AttributeStatSnapshot baseSnapshot)
    {
        if (!baseSnapshot.IsValid)
            return baseSnapshot;

        if (equipmentManager == null)
            LoadEquipmentManager();

        StatAccumulator attack = new(baseSnapshot.Attack);
        StatAccumulator armor = new(baseSnapshot.Armor);
        StatAccumulator maxHealth = new(baseSnapshot.MaxHealth);
        StatAccumulator critChance = new(baseSnapshot.CritChance);
        StatAccumulator critDamage = new(baseSnapshot.CritDamage);

        if (equipmentManager != null)
        {
            foreach (EquipmentSlotData slot in equipmentManager.EquippedSlots)
            {
                ItemDefinition item = slot?.Item;
                if (item == null) continue;

                if (slot.EquipmentInstance != null && slot.EquipmentInstance.IsValid)
                    ApplyModifiers(slot.EquipmentInstance.BuildModifiers(item));
                else
                    ApplyModifiers(item.BuildEquipmentModifiers(equipmentManager.GetUpgradeLevel(slot.SlotType)));
            }
        }

        ApplyAttributePointBonus(StatType.Attack, ref attack);
        ApplyAttributePointBonus(StatType.Armor, ref armor);
        ApplyAttributePointBonus(StatType.MaxHealth, ref maxHealth);
        ApplyAttributePointBonus(StatType.CritChance, ref critChance);
        ApplyAttributePointBonus(StatType.CritDamage, ref critDamage);

        float finalMaxHealth = maxHealth.FinalValue;
        float healthRatio = baseSnapshot.MaxHealth > 0f
            ? Mathf.Clamp01(baseSnapshot.CurrentHealth / baseSnapshot.MaxHealth)
            : 1f;

        return new AttributeStatSnapshot(
            attack.FinalValue,
            armor.FinalValue,
            finalMaxHealth * healthRatio,
            finalMaxHealth,
            critChance.FinalValue,
            critDamage.FinalValue);

        void ApplyModifiers(System.Collections.Generic.IEnumerable<StatModifier> modifiers)
        {
            if (modifiers == null) return;

            foreach (StatModifier modifier in modifiers)
            {
                if (modifier == null || !modifier.IsEnabled) continue;

                switch (modifier.StatType)
                {
                    case StatType.Attack:
                        attack.Add(modifier);
                        break;
                    case StatType.Armor:
                        armor.Add(modifier);
                        break;
                    case StatType.MaxHealth:
                        maxHealth.Add(modifier);
                        break;
                    case StatType.CritChance:
                        critChance.Add(modifier);
                        break;
                    case StatType.CritDamage:
                        critDamage.Add(modifier);
                        break;
                }
            }
        }

        void ApplyAttributePointBonus(StatType statType, ref StatAccumulator accumulator)
        {
            float bonus = PlayerAttributePointStorage.GetBonusValue(statType);
            if (Mathf.Approximately(bonus, 0f)) return;

            accumulator.Add(new StatModifier(statType, ModifierType.Flat, bonus));
        }
    }

    private void SubscribeStatEvents()
    {
        if (characterStat == null)
            LoadCharacterStat();

        if (characterStat == null) return;

        characterStat.OnHealthChanged -= HandleHealthChanged;
        characterStat.OnHealthChanged += HandleHealthChanged;
        characterStat.OnStatChanged -= HandleStatChanged;
        characterStat.OnStatChanged += HandleStatChanged;
    }

    private void UnsubscribeStatEvents()
    {
        if (characterStat == null) return;

        characterStat.OnHealthChanged -= HandleHealthChanged;
        characterStat.OnStatChanged -= HandleStatChanged;
    }

    private void SubscribeEquipmentEvents()
    {
        if (equipmentManager == null)
            LoadEquipmentManager();

        if (equipmentManager == null) return;

        equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
    }

    private void UnsubscribeEquipmentEvents()
    {
        if (equipmentManager == null) return;

        equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
    }

    private void HandleHealthChanged(float currentHealth)
    {
        Refresh();
    }

    private void HandleStatChanged(StatType statType)
    {
        Refresh();
    }

    private void HandleEquipmentChanged()
    {
        Refresh();
    }

    private struct StatAccumulator
    {
        private readonly float baseValue;
        private float flat;
        private float percentAdd;
        private float percentMultiply;

        public float FinalValue => (baseValue + flat) * (1f + percentAdd) * percentMultiply;

        public StatAccumulator(float baseValue)
        {
            this.baseValue = baseValue;
            flat = 0f;
            percentAdd = 0f;
            percentMultiply = 1f;
        }

        public void Add(StatModifier modifier)
        {
            float amount = modifier.GetEffectiveValue();

            switch (modifier.ModifierType)
            {
                case ModifierType.Flat:
                    flat += amount;
                    break;
                case ModifierType.PercentAdd:
                    percentAdd += amount;
                    break;
                case ModifierType.PercentMultiply:
                    percentMultiply *= 1f + amount;
                    break;
            }
        }
    }
}
