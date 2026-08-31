using System;
using UnityEngine;

public class MainMenuPanel : BaseMonoBehaviour
{
    public event Action OnCreateCharacterRequested;
    public event Action OnDeleteCharacterRequested;
    public event Action OnQuitRequested;

    [SerializeField] private ButtonStartGame playButton;
    [SerializeField] private ButtonQuitGame quitButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        BindButtonEvents();
    }

    protected override void OnDisable()
    {
        UnbindButtonEvents();
        base.OnDisable();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();

        playButton ??= GetComponentInChildren<ButtonStartGame>(true);
        quitButton ??= GetComponentInChildren<ButtonQuitGame>(true);
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
        if (playButton != null)
        {
            playButton.OnClicked -= RequestCreateCharacter;
            playButton.OnClicked += RequestCreateCharacter;
        }

        if (quitButton != null)
        {
            quitButton.OnClicked -= RequestQuitGame;
            quitButton.OnClicked += RequestQuitGame;
        }
    }

    private void UnbindButtonEvents()
    {
        if (playButton != null)
            playButton.OnClicked -= RequestCreateCharacter;

        if (quitButton != null)
            quitButton.OnClicked -= RequestQuitGame;
    }
}
