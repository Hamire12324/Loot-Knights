using UnityEngine;
using Photon.Pun;

public class HeroCtrl : CharacterCtrl
{
    public static HeroCtrl Local;

    [SerializeField] private PhotonView photonView;
    [SerializeField] private CreatedCharacterData profile;
    public CreatedCharacterData Profile => profile;
    protected override void Awake()
    {
        base.Awake();

        SetAsLocalIfOwned();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetAsLocalIfOwned();
    }

    protected override void OnDestroy()
    {
        if (Local == this)
        {
            Local = null;
        }

        base.OnDestroy();
    }

    private void SetAsLocalIfOwned()
    {
        if (CanBeLocalHero())
        {
            Local = this;
        }
    }

    private bool CanBeLocalHero()
    {
        return photonView == null
            || !PhotonNetwork.InRoom
            || photonView.IsMine;
    }

    protected override void ResetValue()
    {
        base.ResetValue();

        this.faction = Faction.Hero;
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();

        this.photonView = GetComponent<PhotonView>();
        LoadHeroLevel();
    }

    private void LoadHeroLevel()
    {
        if (characterLevel == null)
            characterLevel = GetComponentInChildren<CharacterLevel>(true);

        if (characterLevel == null)
            characterLevel = gameObject.AddComponent<CharacterLevel>();
    }
    public void ApplyProfile(CreatedCharacterData characterData)
    {
        profile = characterData;

        if (characterData != null && !string.IsNullOrWhiteSpace(characterData.CharacterName))
        {
            gameObject.name = characterData.CharacterName;
        }
    }
    public static HeroCtrl GetLocal()
    {
        if (Local != null) return Local;

        HeroCtrl hero = FindAnyObjectByType<HeroCtrl>(FindObjectsInactive.Exclude);

        if (hero != null && hero.CanBeLocalHero())
        {
            Local = hero;
        }

        return Local;
    }
}
