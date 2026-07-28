using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[DefaultExecutionOrder(-10000)]
public class HeroGameplaySpawner : BaseMonoBehaviour
{
    [SerializeField] private HeroCtrl defaultHeroPrefab;
    [SerializeField] private CharacterClassHeroPrefab[] classPrefabs;
    [SerializeField] private SkillTreeDefinition skillTree;
    [SerializeField] private SkillTreeDefinition elementalSkillTree;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform spawnedHeroParent;
    [SerializeField] private bool destroySceneHeroesBeforeSpawn = true;
    [SerializeField] private bool bindCinemachineCameras = true;

    public HeroCtrl SpawnedHero { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.SpawnHeroFromProfile();
    }

    public HeroCtrl SpawnHeroFromProfile()
    {
        CreatedCharacterData characterData = CharacterProfileStorage.Load();
        CharacterClass characterClass = characterData != null
            ? characterData.CharacterClass
            : CharacterClass.Knight;

        HeroCtrl heroPrefab = GetHeroPrefab(characterClass);

        Vector3 spawnPosition = GetSpawnPosition();

        if (destroySceneHeroesBeforeSpawn) DestroySceneHeroes();

        Transform parent = spawnedHeroParent != null ? spawnedHeroParent : null;
        SpawnedHero = Instantiate(heroPrefab, spawnPosition, Quaternion.identity, parent);
        if (characterData != null)
            SpawnedHero.ApplyProfile(characterData);

        PlayerEquipmentManager.InstanceOrNull?.ApplyToHero(SpawnedHero);
        ApplySkillLoadout(SpawnedHero, characterClass);
        BindCinemachineCameras(SpawnedHero.transform);

        return SpawnedHero;
    }

    private HeroCtrl GetHeroPrefab(CharacterClass characterClass)
    {
        if (classPrefabs != null)
        {
            foreach (CharacterClassHeroPrefab classPrefab in classPrefabs)
            {
                if (classPrefab == null) continue;
                if (classPrefab.CharacterClass != characterClass) continue;
                if (classPrefab.HeroPrefab == null) continue;

                return classPrefab.HeroPrefab;
            }
        }

        return defaultHeroPrefab;
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }

        HeroCtrl sceneHero = FindAnyObjectByType<HeroCtrl>(FindObjectsInactive.Include);
        return sceneHero != null ? sceneHero.transform.position : Vector3.zero;
    }

    private void DestroySceneHeroes()
    {
        HeroCtrl[] sceneHeroes = FindObjectsByType<HeroCtrl>(FindObjectsInactive.Include);

        foreach (HeroCtrl sceneHero in sceneHeroes)
        {
            if (sceneHero == null) continue;

            Destroy(sceneHero.gameObject);
        }
    }

    private int BindCinemachineCameras(Transform heroTransform)
    {
        if (!bindCinemachineCameras) return 0;

        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include);
        int boundCount = 0;

        foreach (CinemachineCamera camera in cameras)
        {
            if (camera == null) continue;

            camera.Follow = heroTransform;
            camera.LookAt = heroTransform;
            boundCount++;
        }

        return boundCount;
    }

    private void ApplySkillLoadout(HeroCtrl hero, CharacterClass characterClass)
    {
        if (hero == null)
            return;

        SkillTreeDefinition classSkillTree = ResolveClassSkillTree(hero);
        if (classSkillTree == null)
            return;

        const int defaultSlotCount = 4;
        PlayerSkillTreeManager skillTreeManager = PlayerSkillTreeManager.Service;
        skillTreeManager.EnsureLevelRewarded(PlayerExperienceStorage.Level);
        ApplySkillTreeStats(hero, classSkillTree);

        IReadOnlyList<SkillTreeDefinition> loadoutTrees = GetLoadoutTrees(classSkillTree);
        foreach (SkillTreeDefinition tree in loadoutTrees)
            skillTreeManager.EnsureUnlockedActiveSkillsEquipped(tree, defaultSlotCount);

        skillTreeManager.ApplyEquippedSkillsToHero(hero, loadoutTrees, defaultSlotCount);

        HeroSkillLoadoutPhotonSync loadoutSync = hero.GetComponent<HeroSkillLoadoutPhotonSync>();
        if (loadoutSync == null)
            loadoutSync = hero.gameObject.AddComponent<HeroSkillLoadoutPhotonSync>();

        loadoutSync.SetSkillTrees(classSkillTree, elementalSkillTree);
        loadoutSync.PublishLocalLoadout();
    }

    private SkillTreeDefinition ResolveClassSkillTree(HeroCtrl hero)
    {
        HeroSkillLoadoutPhotonSync loadoutSync = hero != null
            ? hero.GetComponent<HeroSkillLoadoutPhotonSync>()
            : null;

        return loadoutSync != null && loadoutSync.SkillTree != null
            ? loadoutSync.SkillTree
            : skillTree;
    }

    private void ApplySkillTreeStats(HeroCtrl hero, SkillTreeDefinition classSkillTree)
    {
        if (hero == null || hero.CharacterStat == null)
            return;

        List<StatModifier> modifiers = new();
        foreach (SkillTreeDefinition tree in GetLoadoutTrees(classSkillTree))
        {
            SkillTreeRuntime runtime = new(tree);
            modifiers.AddRange(runtime.CreateStatModifiers());
        }

        hero.CharacterStat.RecalculateSkillTree(modifiers);
    }

    private IReadOnlyList<SkillTreeDefinition> GetLoadoutTrees(SkillTreeDefinition classSkillTree)
    {
        List<SkillTreeDefinition> trees = new();
        if (classSkillTree != null)
            trees.Add(classSkillTree);

        if (elementalSkillTree != null && !trees.Contains(elementalSkillTree))
            trees.Add(elementalSkillTree);

        return trees;
    }
}
