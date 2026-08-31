using System;
using UnityEngine;
public class LobbyUIController : BaseMonoBehaviour
{
    private CharacterMenuPanel characterMenuPanel;

    public event Action StoreRequested;
    public event Action FriendsRequested;
    public event Action MessagesRequested;
    public event Action RankingRequested;
    public event Action AddCoinsRequested;
    public event Action AddDiamondsRequested;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        characterMenuPanel ??= GetComponentInChildren<CharacterMenuPanel>(true);
        characterMenuPanel ??= FindAnyObjectByType<CharacterMenuPanel>(FindObjectsInactive.Include);

    }

    public void OpenHero()
    {
        characterMenuPanel?.ShowSection(CharacterMenuSection.Attribute);
    }

    public void OpenArmour()
    {
        characterMenuPanel?.ShowSection(CharacterMenuSection.Strengthen);
    }

    public void OpenBackpack()
    {
        characterMenuPanel?.ShowEquipmentView();
    }

    public void OpenStore() => StoreRequested?.Invoke();
    public void OpenFriends() => FriendsRequested?.Invoke();
    public void OpenMessages() => MessagesRequested?.Invoke();
    public void OpenRanking() => RankingRequested?.Invoke();
    public void OpenAddCoins() => AddCoinsRequested?.Invoke();
    public void OpenAddDiamonds() => AddDiamondsRequested?.Invoke();
}
