using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : BaseMonoBehaviour
{
    public event Action OnContinueRequested;
    public event Action OnCreateCharacterRequested;
    public event Action OnDeleteCharacterRequested;
    public event Action OnQuitRequested;

    [Header("Status")]
    [SerializeField] private TMP_Text characterStatusText;
    [SerializeField] private Button continueButton;

    private Button createCharacterButton;
    private Button quitButton;
    protected override void Start()
    {
        base.Start();

        this.BindButtonEvents();
        this.Refresh();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        this.Refresh();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (continueButton == null)
        {
            ButtonContinueGame continueGameButton = GetComponentInChildren<ButtonContinueGame>(true);

            if (continueGameButton != null)
            {
                continueButton = continueGameButton.GetComponent<Button>();
            }
        }

        this.LoadButtonsByName();
    }
    protected virtual void LoadButtonsByName()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button foundButton in buttons)
        {
            string buttonName = foundButton.name.ToLowerInvariant();

            if (continueButton == null && (buttonName.Contains("continue") || buttonName.Contains("resume")))
            {
                continueButton = foundButton;
                continue;
            }

            if (createCharacterButton == null && (buttonName.Contains("start") || buttonName.Contains("new")))
            {
                createCharacterButton = foundButton;
                continue;
            }

            if (quitButton == null && buttonName.Contains("quit"))
            {
                quitButton = foundButton;
            }
        }
    }
    private void Refresh()
    {
        if (characterStatusText == null) return;

        CreatedCharacterData character = CharacterProfileStorage.Load();
        bool hasCharacter = character != null;

        characterStatusText.text = character == null
            ? "Chua co nhan vat"
            : character.CharacterName + " - " + character.CharacterClass;

        if (continueButton != null)
        {
            continueButton.interactable = hasCharacter;
        }
    }

    public void RequestContinueGame()
    {
        OnContinueRequested?.Invoke();
    }
    public void RequestCreateCharacter()
    {
        OnCreateCharacterRequested?.Invoke();
    }

    public void RequestDeleteCharacter()
    {
        OnDeleteCharacterRequested?.Invoke();
    }

    public void RequestQuitGame()
    {
        OnQuitRequested?.Invoke();
    }
    private void BindButtonEvents()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(RequestContinueGame);
            continueButton.onClick.AddListener(RequestContinueGame);
        }

        if (createCharacterButton != null)
        {
            createCharacterButton.onClick.RemoveListener(RequestCreateCharacter);
            createCharacterButton.onClick.AddListener(RequestCreateCharacter);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(RequestQuitGame);
            quitButton.onClick.AddListener(RequestQuitGame);
        }
    }
}
