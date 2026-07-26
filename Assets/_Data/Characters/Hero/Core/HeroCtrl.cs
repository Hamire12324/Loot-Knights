using UnityEngine;
using Photon.Pun;

public class HeroCtrl : CharacterCtrl
{
    public static HeroCtrl Local;

    [SerializeField] private PhotonView photonView;
    [SerializeField] private CreatedCharacterData profile;
    public CreatedCharacterData Profile => profile;
    [SerializeField] private HeroLevel heroLevel;
    public HeroLevel HeroLevel => heroLevel;
    [SerializeField] private HeroSkillController heroSkillController;
    public HeroSkillController HeroSkillController => heroSkillController;
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
    }

    protected override void LoadCharacterLevel()
    {
        LoadHeroLevel();
    }

    protected override void LoadCharacterSkillController()
    {
        LoadHeroSkillController();
    }

    private void LoadHeroLevel()
    {
        if (heroLevel == null)
            heroLevel = FindChildComponent<HeroLevel>();

        if (heroLevel == null)
            Debug.LogError($"{nameof(HeroCtrl)} requires a child {nameof(HeroLevel)}.", gameObject);

        characterLevel = heroLevel;
    }

    private void LoadHeroSkillController()
    {
        if (heroSkillController == null)
            heroSkillController = FindChildComponent<HeroSkillController>();

        if (heroSkillController == null)
            Debug.LogError($"{nameof(HeroCtrl)} requires a child {nameof(HeroSkillController)}.", gameObject);

        characterSkillController = heroSkillController;
    }

    private T FindChildComponent<T>() where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            if (component != null && component.transform != transform)
                return component;
        }

        return null;
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
