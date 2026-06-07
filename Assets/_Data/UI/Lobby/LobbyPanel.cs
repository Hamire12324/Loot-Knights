using System;
using UnityEngine;
using UnityEngine.Events;

public class LobbyPanel : BaseMonoBehaviour
{
    public event Action OnReadyGoRequested;

    [Header("Views")]
    [SerializeField] private LobbyProfileView profileView;
    [SerializeField] private LobbyCurrencyView currencyView;
    [SerializeField] private CharacterMenuPanel characterMenuPanel;

    [Header("Buttons")]
    [SerializeField] private ButtonHero heroButton;
    [SerializeField] private ButtonArmour armourButton;
    [SerializeField] private ButtonBackpack backpackButton;
    [SerializeField] private ButtonStore storeButton;
    [SerializeField] private ButtonFriends friendsButton;
    [SerializeField] private ButtonMessages messagesButton;
    [SerializeField] private ButtonRanking rankingButton;
    [SerializeField] private ButtonSettings settingsButton;
    [SerializeField] private ButtonAddCoins addCoinsButton;
    [SerializeField] private ButtonAddDiamonds addDiamondsButton;
    [SerializeField] private ButtonStartGameplay readyGoButton;

    [Header("Events")]
    [SerializeField] private UnityEvent onHeroRequested = new UnityEvent();
    [SerializeField] private UnityEvent onArmourRequested = new UnityEvent();
    [SerializeField] private UnityEvent onBackpackRequested = new UnityEvent();
    [SerializeField] private UnityEvent onStoreRequested = new UnityEvent();
    [SerializeField] private UnityEvent onFriendsRequested = new UnityEvent();
    [SerializeField] private UnityEvent onMessagesRequested = new UnityEvent();
    [SerializeField] private UnityEvent onRankingRequested = new UnityEvent();
    [SerializeField] private UnityEvent onSettingsRequested = new UnityEvent();
    [SerializeField] private UnityEvent onAddCoinsRequested = new UnityEvent();
    [SerializeField] private UnityEvent onAddDiamondsRequested = new UnityEvent();
    [SerializeField] private UnityEvent onReadyGoRequested = new UnityEvent();

    protected override void OnEnable()
    {
        base.OnEnable();
        Refresh();
    }

    protected override void Start()
    {
        base.Start();
        Refresh();
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();

        LoadViews();
        LoadButtons();
        SetupButtons();
    }

    public void Refresh()
    {
        profileView?.Refresh();
        currencyView?.Refresh();
    }

    public void OpenHero()
    {
        LoadCharacterMenuPanel();
        characterMenuPanel?.ShowSection(CharacterMenuSection.Attribute);
        onHeroRequested.Invoke();
    }
    public void OpenArmour()
    {
        LoadCharacterMenuPanel();
        characterMenuPanel?.ShowSection(CharacterMenuSection.Strengthen);
        onArmourRequested.Invoke();
    }
    public void OpenBackpack()
    {
        LoadCharacterMenuPanel();
        characterMenuPanel?.ShowInventory();
        onBackpackRequested.Invoke();
    }
    public void OpenStore() => onStoreRequested.Invoke();
    public void OpenFriends() => onFriendsRequested.Invoke();
    public void OpenMessages() => onMessagesRequested.Invoke();
    public void OpenRanking() => onRankingRequested.Invoke();
    public void OpenSettings() => onSettingsRequested.Invoke();
    public void OpenAddCoins() => onAddCoinsRequested.Invoke();
    public void OpenAddDiamonds() => onAddDiamondsRequested.Invoke();
    public void ReadyGo()
    {
        OnReadyGoRequested?.Invoke();
        onReadyGoRequested.Invoke();
    }

    private void LoadViews()
    {
        if (profileView == null)
        {
            profileView = GetComponentInChildren<LobbyProfileView>(true);
        }

        if (currencyView == null)
        {
            currencyView = GetComponentInChildren<LobbyCurrencyView>(true);
        }

        LoadCharacterMenuPanel();
    }

    private void LoadCharacterMenuPanel()
    {
        if (characterMenuPanel != null) return;

        characterMenuPanel = GetComponentInChildren<CharacterMenuPanel>(true);
        if (characterMenuPanel != null) return;

        characterMenuPanel = FindAnyObjectByType<CharacterMenuPanel>(FindObjectsInactive.Include);
    }

    private void LoadButtons()
    {
        LoadButton(ref heroButton);
        LoadButton(ref armourButton);
        LoadButton(ref backpackButton);
        LoadButton(ref storeButton);
        LoadButton(ref friendsButton);
        LoadButton(ref messagesButton);
        LoadButton(ref rankingButton);
        LoadButton(ref settingsButton);
        LoadButton(ref addCoinsButton);
        LoadButton(ref addDiamondsButton);
        LoadButton(ref readyGoButton);
    }

    private void LoadButton<T>(ref T target) where T : ButtonLobbySection
    {
        if (target != null) return;

        target = GetComponentInChildren<T>(true);
    }

    private void SetupButtons()
    {
        SetupButton(heroButton);
        SetupButton(armourButton);
        SetupButton(backpackButton);
        SetupButton(storeButton);
        SetupButton(friendsButton);
        SetupButton(messagesButton);
        SetupButton(rankingButton);
        SetupButton(settingsButton);
        SetupButton(addCoinsButton);
        SetupButton(addDiamondsButton);
        SetupButton(readyGoButton);
    }

    private void SetupButton(ButtonLobbySection target)
    {
        if (target == null) return;

        target.SetLobbyPanel(this);
    }
}
