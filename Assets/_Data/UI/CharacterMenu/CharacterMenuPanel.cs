using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMenuPanel : BaseMonoBehaviour
{
    [SerializeField] private CharacterMenuSection defaultSection = CharacterMenuSection.Attribute;
    [SerializeField] private List<CharacterMenuSectionBinding> sections = new();
    [SerializeField] private Button closeButton;
    [SerializeField] private CharacterMenuEquipmentPanel equipmentView;
    [SerializeField] private bool hideOnStart = true;
    [Header("Skill Trees")]
    [SerializeField] private CharacterClassSkillTreeBinding[] classSkillTrees;
    [SerializeField] private SkillTreeDefinition elementalSkillTree;
    [SerializeField] private string classSkillTreeLabel = "CLASS";
    [SerializeField] private string elementalSkillTreeLabel = "ELEMENT";

    private bool initialized;
    private bool openedBeforeStart;

    public CharacterMenuSection CurrentSection { get; private set; }

    protected override void Start()
    {
        base.Start();

        Initialize();

        if (!openedBeforeStart)
            ShowSection(defaultSection, activatePanel: false);

        if (hideOnStart && !openedBeforeStart)
            Hide();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadCloseButton();
        LoadTabButtons();
        LoadEquipmentView();
    }

    public void Show()
    {
        ShowSection(CurrentSection);
    }

    public void ShowInventory()
    {
        ShowSection(CharacterMenuSection.Inventory);
    }

    public void ShowSection(CharacterMenuSection section)
    {
        ShowSection(section, activatePanel: true);
    }

    public void Hide()
    {
        Initialize();
        SetActive(false);
    }

    public IReadOnlyList<SkillTreeDefinition> GetSkillTreesForCurrentProfile()
    {
        CreatedCharacterData savedCharacter = CharacterProfileStorage.Load();
        SkillTreeDefinition classSkillTree = savedCharacter != null
            ? FindClassSkillTree(savedCharacter.CharacterClass)
            : null;

        return GetSkillTrees(classSkillTree);
    }

    private void ShowSection(CharacterMenuSection section, bool activatePanel)
    {
        if (activatePanel)
            openedBeforeStart = true;

        Initialize();

        CurrentSection = section;

        if (activatePanel)
            SetActive(true);

        foreach (CharacterMenuSectionBinding binding in sections)
        {
            if (binding == null) continue;

            bool selected = binding.Section == section;
            if (binding.ViewRoot != null)
            {
                binding.ViewRoot.SetActive(selected);
                RefreshView(binding.ViewRoot, selected, binding.Section);
            }

            binding.TabButton?.SetSelected(selected);
        }

        RefreshEquipmentView();
    }

    private void Initialize()
    {
        if (initialized) return;

        LoadCloseButton();
        LoadTabButtons();
        LoadEquipmentView();
        BindCloseButton();
        initialized = true;
    }

    private void RefreshView(GameObject viewRoot, bool selected, CharacterMenuSection section)
    {
        if (!selected || viewRoot == null) return;

        InventoryView inventoryView = viewRoot.GetComponentInChildren<InventoryView>(true);
        if (inventoryView == null && section == CharacterMenuSection.Inventory)
            inventoryView = viewRoot.AddComponent<InventoryPanel>();

        inventoryView?.Refresh();

        AttributeView attributeView = viewRoot.GetComponentInChildren<AttributeView>(true);
        if (attributeView == null && section == CharacterMenuSection.Attribute)
            attributeView = viewRoot.AddComponent<AttributeView>();

        attributeView?.Refresh();

        if (section == CharacterMenuSection.Strengthen)
        {
            CharacterStatUpgradePanel upgradePanel = viewRoot.GetComponentInChildren<CharacterStatUpgradePanel>(true);
            upgradePanel?.Refresh();

            ForgeMarketUIController forgePanel = viewRoot.GetComponentInChildren<ForgeMarketUIController>(true);
            forgePanel?.Refresh();
        }

        if (section == CharacterMenuSection.Skill)
        {
            SkillTreeView skillTreeView = viewRoot.GetComponentInChildren<SkillTreeView>(true);
            ConfigureSkillTreeView(skillTreeView);
            skillTreeView?.Refresh();
        }
    }

    private void RefreshEquipmentView()
    {
        LoadEquipmentView();

        if (equipmentView == null) return;

        bool shouldShowEquipment = CurrentSection != CharacterMenuSection.Skill &&
                                   CurrentSection != CharacterMenuSection.Elemental;
        equipmentView.gameObject.SetActive(shouldShowEquipment);

        if (shouldShowEquipment)
            equipmentView.Refresh();
    }

    private void ConfigureSkillTreeView(SkillTreeView skillTreeView)
    {
        if (skillTreeView == null || elementalSkillTree == null)
            return;

        SkillTreeDefinition classSkillTree = ResolveClassSkillTree(skillTreeView);
        skillTreeView.SetSkillTrees(
            classSkillTree,
            elementalSkillTree,
            classSkillTreeLabel,
            elementalSkillTreeLabel);
    }

    private SkillTreeDefinition ResolveClassSkillTree(SkillTreeView skillTreeView)
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        HeroSkillLoadoutPhotonSync loadoutSync = hero != null
            ? hero.GetComponent<HeroSkillLoadoutPhotonSync>()
            : null;

        if (loadoutSync != null && loadoutSync.SkillTree != null)
            return loadoutSync.SkillTree;

        CreatedCharacterData savedCharacter = CharacterProfileStorage.Load();
        if (savedCharacter != null)
        {
            SkillTreeDefinition savedClassTree = FindClassSkillTree(savedCharacter.CharacterClass);
            if (savedClassTree != null)
                return savedClassTree;
        }

        return skillTreeView.PrimarySkillTree;
    }

    private SkillTreeDefinition FindClassSkillTree(CharacterClass characterClass)
    {
        if (classSkillTrees == null)
            return null;

        foreach (CharacterClassSkillTreeBinding binding in classSkillTrees)
        {
            if (binding == null) continue;
            if (binding.CharacterClass != characterClass) continue;

            return binding.SkillTree;
        }

        return null;
    }

    private IReadOnlyList<SkillTreeDefinition> GetSkillTrees(SkillTreeDefinition classSkillTree)
    {
        List<SkillTreeDefinition> trees = new();
        if (classSkillTree != null)
            trees.Add(classSkillTree);

        if (elementalSkillTree != null && !trees.Contains(elementalSkillTree))
            trees.Add(elementalSkillTree);

        return trees;
    }

    private void LoadCloseButton()
    {
        if (closeButton != null) return;

        Transform close = transform.Find("CloseButton");
        if (close != null)
            closeButton = close.GetComponent<Button>();
    }

    private void LoadTabButtons()
    {
        foreach (CharacterMenuTabButton tabButton in GetComponentsInChildren<CharacterMenuTabButton>(true))
        {
            tabButton.SetPanel(this);
        }
    }

    private void LoadEquipmentView()
    {
        if (equipmentView != null) return;

        Transform equipment = transform.Find("CharacterEquipmentPanel");
        if (equipment == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == null) continue;
                if (child.name != "CharacterEquipmentPanel") continue;

                equipment = child;
                break;
            }
        }

        if (equipment == null) return;

        equipmentView = equipment.GetComponent<CharacterMenuEquipmentPanel>();
        if (equipmentView == null)
            equipmentView = equipment.gameObject.AddComponent<CharacterMenuEquipmentPanel>();
    }

    private void BindCloseButton()
    {
        if (closeButton == null) return;

        closeButton.onClick.RemoveListener(Hide);
        closeButton.onClick.AddListener(Hide);
    }

    private void OnValidate()
    {
        sections ??= new List<CharacterMenuSectionBinding>();
    }
}

[System.Serializable]
public sealed class CharacterClassSkillTreeBinding
{
    [SerializeField] private CharacterClass characterClass = CharacterClass.Knight;
    [SerializeField] private SkillTreeDefinition skillTree;

    public CharacterClass CharacterClass => characterClass;
    public SkillTreeDefinition SkillTree => skillTree;
}
