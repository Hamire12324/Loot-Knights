using System;
using UnityEngine;

public class LobbyPanel : BaseMonoBehaviour
{
    public event Action OnReadyGoRequested;

    [SerializeField] private LobbyProfileView profileView;
    [SerializeField] private LobbyCurrencyView currencyView;
    [SerializeField] private LobbyUIController lobbyUIController;

    protected override void OnEnable()
    {
        base.OnEnable();
        Refresh();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();

        profileView ??= GetComponentInChildren<LobbyProfileView>(true);
        currencyView ??= GetComponentInChildren<LobbyCurrencyView>(true);
        lobbyUIController ??= GetComponent<LobbyUIController>();
        lobbyUIController ??= gameObject.AddComponent<LobbyUIController>();
    }

    public void Refresh()
    {
        profileView?.Refresh();
        currencyView?.Refresh();
    }

    public void OpenHero()
    {
        lobbyUIController?.OpenHero();
    }
    public void OpenArmour()
    {
        lobbyUIController?.OpenArmour();
    }
    public void OpenBackpack()
    {
        lobbyUIController?.OpenBackpack();
    }
    public void ReadyGo()
    {
        OnReadyGoRequested?.Invoke();
    }
}
