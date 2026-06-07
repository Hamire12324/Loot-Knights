using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreationPanel : BaseMonoBehaviour
{
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
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    protected override void Start()
    {
        base.Start();

        BindActionButtons();
        SetupRoleButtons();
        SelectRole(selectedRoleIndex);
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadNameInput();
        LoadRoleTexts();
        LoadPreviewImage();
        LoadRoleButtons();
        LoadSelectedRoleMarker();
        LoadSkillIcons();
        LoadStatBars();
        LoadActionButtons();
    }

    public void CreateCharacter()
    {
        CharacterRoleDefinition selectedRole = GetSelectedRole();
        string characterName = nameInput != null ? nameInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(characterName))
        {
            characterName = selectedRole != null ? selectedRole.RoleName : string.Empty;
        }

        if (string.IsNullOrEmpty(characterName))
        {
            return;
        }

        CharacterClass characterClass = selectedRole != null
            ? selectedRole.CharacterClass
            : CharacterClass.Knight;

        OnCharacterCreated?.Invoke(new CreatedCharacterData(characterName, characterClass));
    }

    public void BackToMenu()
    {
        OnBackRequested?.Invoke();
    }

    public void SelectRole(int roleIndex)
    {
        if (roles == null || roles.Length == 0)
        {
            selectedRoleIndex = Mathf.Max(0, roleIndex);
            UpdateRoleButtons();
            return;
        }

        selectedRoleIndex = Mathf.Clamp(roleIndex, 0, roles.Length - 1);
        CharacterRoleDefinition selectedRole = roles[selectedRoleIndex];

        UpdateRoleInfo(selectedRole);
        UpdateRoleButtons();
    }

    private void LoadNameInput()
    {
        if (nameInput != null) return;

        nameInput = GetComponentInChildren<TMP_InputField>(true);
    }

    private void LoadRoleTexts()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
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

        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            string imageName = image.name.ToLowerInvariant();

            if (imageName.Contains("characterpreviewimage"))
            {
                characterPreviewImage = image;
                return;
            }
        }
    }

    private void LoadRoleButtons()
    {
        if (roleButtons != null && roleButtons.Length > 0) return;

        roleButtons = GetComponentsInChildren<CharacterRoleButton>(true);
    }

    private void LoadSelectedRoleMarker()
    {
        if (selectedRoleMarker != null) return;

        RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rectTransform in rectTransforms)
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
        if (skillIcons != null && skillIcons.Length > 0) return;

        skillIcons = GetComponentsInChildren<CharacterSkillIcon>(true);
    }

    private void LoadStatBars()
    {
        UIStatBar[] statBars = GetComponentsInChildren<UIStatBar>(true);

        foreach (UIStatBar statBar in statBars)
        {
            string statName = statBar.name.ToLowerInvariant();

            if (attackBar == null && statName.Contains("attack"))
            {
                attackBar = statBar;
                continue;
            }

            if (defenceBar == null && (statName.Contains("defence") || statName.Contains("defense")))
            {
                defenceBar = statBar;
                continue;
            }

            if (vitalityBar == null && (statName.Contains("vitality") || statName.Contains("health") || statName.Contains("hp")))
            {
                vitalityBar = statBar;
                continue;
            }

            if (speedBar == null && statName.Contains("speed"))
            {
                speedBar = statBar;
            }
        }
    }

    private void LoadActionButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            string buttonName = button.name.ToLowerInvariant();

            if (startButton == null && (buttonName.Contains("start") || buttonName.Contains("confirm") || buttonName.Contains("create")))
            {
                startButton = button;
                continue;
            }

            if (backButton == null && buttonName.Contains("back"))
            {
                backButton = button;
            }
        }
    }

    private void BindActionButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(CreateCharacter);
            startButton.onClick.AddListener(CreateCharacter);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackToMenu);
            backButton.onClick.AddListener(BackToMenu);
        }
    }

    private void SetupRoleButtons()
    {
        if (roleButtons == null) return;

        for (int i = 0; i < roleButtons.Length; i++)
        {
            CharacterRoleDefinition role = roles != null && i < roles.Length ? roles[i] : null;
            Sprite portrait = role != null ? role.Portrait : null;

            roleButtons[i].Setup(this, i, portrait);
        }
    }

    private void UpdateRoleInfo(CharacterRoleDefinition role)
    {
        if (roleNameText != null)
        {
            roleNameText.text = role.RoleName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = role.Description;
        }

        if (characterPreviewImage != null)
        {
            characterPreviewImage.sprite = role.FullBodySprite;
            characterPreviewImage.enabled = role.FullBodySprite != null;
        }

        if (attackBar != null) attackBar.SetValue(role.Attack);
        if (defenceBar != null) defenceBar.SetValue(role.Defence);
        if (vitalityBar != null) vitalityBar.SetValue(role.Vitality);
        if (speedBar != null) speedBar.SetValue(role.Speed);

        UpdateSkillIcons(role);
    }

    private void UpdateRoleButtons()
    {
        if (roleButtons == null) return;

        for (int i = 0; i < roleButtons.Length; i++)
        {
            bool isSelected = i == selectedRoleIndex;

            if (isSelected)
            {
                MoveSelectedRoleMarker(roleButtons[i]);
            }
        }
    }

    private void MoveSelectedRoleMarker(CharacterRoleButton selectedButton)
    {
        if (selectedRoleMarker == null)
        {
            return;
        }

        if (selectedButton == null)
        {
            return;
        }

        if (selectedButton.RectTransform == null)
        {
            return;
        }

        selectedRoleMarker.gameObject.SetActive(true);

        if (selectedRoleMarker.parent == selectedButton.RectTransform.parent)
        {
            selectedRoleMarker.anchoredPosition = selectedButton.RectTransform.anchoredPosition;
        }
        else
        {
            selectedRoleMarker.position = selectedButton.RectTransform.position;
        }
    }

    private void UpdateSkillIcons(CharacterRoleDefinition role)
    {
        if (skillIcons == null) return;

        for (int i = 0; i < skillIcons.Length; i++)
        {
            Sprite icon = role.SkillIcons != null && i < role.SkillIcons.Length ? role.SkillIcons[i] : null;
            skillIcons[i].SetIcon(icon);
        }
    }

    private CharacterRoleDefinition GetSelectedRole()
    {
        if (roles == null || roles.Length == 0) return null;

        selectedRoleIndex = Mathf.Clamp(selectedRoleIndex, 0, roles.Length - 1);
        return roles[selectedRoleIndex];
    }

}
