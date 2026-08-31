using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreationPanel : BaseMonoBehaviour
{
    private const string RoleResourcesPath = "CharacterRoles";

    public event Action<CreatedCharacterData> OnCharacterCreated;
    public event Action OnBackRequested;

    [Header("Role Data")]
    [SerializeField] private CharacterRoleDefinition[] roles;
    [SerializeField] private int selectedRoleIndex;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField nameInput;

    [Header("Role Preview")]
    [SerializeField] private TMP_Text roleNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image characterPreviewImage;
    [SerializeField] private CharacterRoleButton[] roleButtons;
    [SerializeField] private RectTransform selectedRoleMarker;
    [SerializeField] private CharacterSkillIcon[] skillIcons;

    [Header("Attributes")]
    [SerializeField] private UIStatBar attackBar;
    [SerializeField] private UIStatBar defenceBar;
    [SerializeField] private UIStatBar vitalityBar;
    [SerializeField] private UIStatBar speedBar;

    [Header("Actions")]
    [SerializeField] private ButtonConfirmCharacterCreation startButton;
    [SerializeField] private ButtonBackCharacterCreation backButton;

    protected override void Start()
    {
        base.Start();

        SetupRoleButtons();
        SelectRole(selectedRoleIndex);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        BindActionButtons();
    }

    protected override void OnDisable()
    {
        UnbindActionButtons();
        base.OnDisable();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        LoadRolesFromResources();
        LoadNameInput();
        LoadRoleTexts();
        LoadPreviewImage();
        LoadRoleButtons();
        LoadSelectedRoleMarker();
        LoadSkillIcons();
        LoadStatBars();
        LoadActionButtons();
    }
    private void LoadRolesFromResources()
    {
        CharacterRoleDefinition[] loadedRoles = CharacterRoleRepository.LoadFromResources(RoleResourcesPath);
        if (loadedRoles.Length > 0)
        {
            roles = loadedRoles;
        }
    }
    private void LoadNameInput()
    {
        if (nameInput == null)
        {
            nameInput = GetComponentInChildren<TMP_InputField>(true);
        }
    }
    private void LoadRoleTexts()
    {
        if (roleNameText != null && descriptionText != null) return;

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            string textName = text.name.ToLowerInvariant();

            if (roleNameText == null && (textName.Contains("role") || textName.Contains("name")))
            {
                roleNameText = text;
                continue;
            }

            if (descriptionText == null && (textName.Contains("description") || textName.Contains("desc")))
            {
                descriptionText = text;
            }
        }
    }
    private void LoadPreviewImage()
    {
        if (characterPreviewImage != null) return;

        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image.name.ToLowerInvariant().Contains("characterpreviewimage"))
            {
                characterPreviewImage = image;
                return;
            }
        }
    }
    private void LoadRoleButtons()
    {
        if (roleButtons == null || roleButtons.Length == 0)
        {
            roleButtons = GetComponentsInChildren<CharacterRoleButton>(true);
        }
    }
    private void LoadSelectedRoleMarker()
    {
        if (selectedRoleMarker != null) return;

        foreach (RectTransform rectTransform in GetComponentsInChildren<RectTransform>(true))
        {
            string markerName = rectTransform.name.ToLowerInvariant();
            if (markerName.Contains("selectedmarker") || markerName.Contains("selectormarker") || markerName.Contains("rolemarker"))
            {
                selectedRoleMarker = rectTransform;
                return;
            }
        }
    }
    private void LoadSkillIcons()
    {
        if (skillIcons == null || skillIcons.Length == 0)
        {
            skillIcons = GetComponentsInChildren<CharacterSkillIcon>(true);
        }
    }
    private void LoadStatBars()
    {
        if (attackBar != null && defenceBar != null && vitalityBar != null && speedBar != null) return;

        foreach (UIStatBar statBar in GetComponentsInChildren<UIStatBar>(true))
        {
            string statName = statBar.name.ToLowerInvariant();

            if (attackBar == null && statName.Contains("attack"))
            {
                attackBar = statBar;
            }
            else if (defenceBar == null && (statName.Contains("defence") || statName.Contains("defense")))
            {
                defenceBar = statBar;
            }
            else if (vitalityBar == null && (statName.Contains("vitality") || statName.Contains("health") || statName.Contains("hp")))
            {
                vitalityBar = statBar;
            }
            else if (speedBar == null && statName.Contains("speed"))
            {
                speedBar = statBar;
            }
        }
    }
    private void LoadActionButtons()
    {
        if (startButton == null)
        {
            startButton = GetComponentInChildren<ButtonConfirmCharacterCreation>(true);
        }

        if (backButton == null)
        {
            backButton = GetComponentInChildren<ButtonBackCharacterCreation>(true);
        }
    }
    public void CreateCharacter()
    {
        string enteredName = nameInput != null ? nameInput.text : string.Empty;

        if (CharacterCreationDataFactory.TryCreate(enteredName, GetSelectedRole(), out CreatedCharacterData characterData))
        {
            OnCharacterCreated?.Invoke(characterData);
        }
    }
    public void BackToMenu()
    {
        OnBackRequested?.Invoke();
    }

    public void SelectRole(int roleIndex)
    {
        if (!HasRoles())
        {
            selectedRoleIndex = Mathf.Max(0, roleIndex);
            UpdateSelectedRoleMarker();
            return;
        }

        selectedRoleIndex = Mathf.Clamp(roleIndex, 0, roles.Length - 1);
        UpdateRolePreview(roles[selectedRoleIndex]);
        UpdateSelectedRoleMarker();
    }

    private void BindActionButtons()
    {
        BindButton(startButton, CreateCharacter);
        BindButton(backButton, BackToMenu);
    }

    private void UnbindActionButtons()
    {
        UnbindButton(startButton, CreateCharacter);
        UnbindButton(backButton, BackToMenu);
    }

    private void SetupRoleButtons()
    {
        if (roleButtons == null) return;

        for (int index = 0; index < roleButtons.Length; index++)
        {
            CharacterRoleButton roleButton = roleButtons[index];
            if (roleButton == null) continue;

            CharacterRoleDefinition role = roles != null && index < roles.Length ? roles[index] : null;
            roleButton.Setup(this, index, role != null ? role.Portrait : null);
        }
    }

    private void UpdateRolePreview(CharacterRoleDefinition role)
    {
        SetText(roleNameText, role != null ? role.RoleName : null);
        SetText(descriptionText, role != null ? role.Description : null);

        if (characterPreviewImage != null)
        {
            characterPreviewImage.sprite = role != null ? role.FullBodySprite : null;
            characterPreviewImage.enabled = characterPreviewImage.sprite != null;
        }

        SetStatBars(role);
        UpdateSkillIcons(role);
    }

    private void SetStatBars(CharacterRoleDefinition role)
    {
        if (attackBar != null) attackBar.SetValue(role != null ? role.Attack : 0f);
        if (defenceBar != null) defenceBar.SetValue(role != null ? role.Defence : 0f);
        if (vitalityBar != null) vitalityBar.SetValue(role != null ? role.Vitality : 0f);
        if (speedBar != null) speedBar.SetValue(role != null ? role.Speed : 0f);
    }

    private void UpdateSelectedRoleMarker()
    {
        if (selectedRoleMarker == null || roleButtons == null || selectedRoleIndex < 0 || selectedRoleIndex >= roleButtons.Length) return;

        RectTransform selectedButtonTransform = roleButtons[selectedRoleIndex] != null ? roleButtons[selectedRoleIndex].RectTransform : null;
        if (selectedButtonTransform == null) return;

        selectedRoleMarker.gameObject.SetActive(true);

        if (selectedRoleMarker.parent == selectedButtonTransform.parent)
        {
            selectedRoleMarker.anchoredPosition = selectedButtonTransform.anchoredPosition;
        }
        else
        {
            selectedRoleMarker.position = selectedButtonTransform.position;
        }
    }

    private void UpdateSkillIcons(CharacterRoleDefinition role)
    {
        if (skillIcons == null) return;

        for (int index = 0; index < skillIcons.Length; index++)
        {
            CharacterSkillIcon skillIcon = skillIcons[index];
            if (skillIcon != null)
            {
                skillIcon.SetIcon(GetSkillIcon(role, index));
            }
        }
    }

    private CharacterRoleDefinition GetSelectedRole()
    {
        if (!HasRoles()) return null;

        selectedRoleIndex = Mathf.Clamp(selectedRoleIndex, 0, roles.Length - 1);
        return roles[selectedRoleIndex];
    }

    private bool HasRoles()
    {
        return roles != null && roles.Length > 0;
    }

    private static void BindButton(ButtonAbstract button, Action action)
    {
        if (button == null) return;

        button.OnClicked -= action;
        button.OnClicked += action;
    }

    private static void UnbindButton(ButtonAbstract button, Action action)
    {
        if (button != null)
        {
            button.OnClicked -= action;
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static Sprite GetSkillIcon(CharacterRoleDefinition role, int index)
    {
        if (role == null) return null;

        if (role.Skills != null && index < role.Skills.Length && role.Skills[index] != null)
        {
            return role.Skills[index].Icon;
        }

        return role.SkillIcons != null && index < role.SkillIcons.Length ? role.SkillIcons[index] : null;
    }
}
