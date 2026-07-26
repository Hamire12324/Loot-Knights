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
        if (heroPrefab == null)
        {
            Debug.LogError($"{nameof(HeroGameplaySpawner)} could not spawn {characterClass}: no hero prefab is assigned.", gameObject);
            return null;
        }

        Debug.Log(
            $"{nameof(HeroGameplaySpawner)} loading profile: {CharacterProfileStorage.GetDebugSummary()}",
            gameObject);
        Debug.Log(
            $"{nameof(HeroGameplaySpawner)} spawning class={characterClass}, prefab={heroPrefab.name}.",
            gameObject);

        Vector3 spawnPosition = GetSpawnPosition();

        if (destroySceneHeroesBeforeSpawn) DestroySceneHeroes();

        Transform parent = spawnedHeroParent != null ? spawnedHeroParent : null;
        SpawnedHero = Instantiate(heroPrefab, spawnPosition, Quaternion.identity, parent);
        if (characterData != null)
            SpawnedHero.ApplyProfile(characterData);

        PlayerEquipmentManager.InstanceOrNull?.ApplyToHero(SpawnedHero);
        ApplySkillLoadout(SpawnedHero, characterClass);
        Debug.Log(
            $"{nameof(HeroGameplaySpawner)} spawned hero={SpawnedHero.name}, skillController={(SpawnedHero.CharacterSkillController != null ? SpawnedHero.CharacterSkillController.name : "null")}, basicSkill={(SpawnedHero.CharacterSkillController != null && SpawnedHero.CharacterSkillController.BasicAttackRuntime != null && SpawnedHero.CharacterSkillController.BasicAttackRuntime.Definition != null ? SpawnedHero.CharacterSkillController.BasicAttackRuntime.Definition.name : "null")}.",
            SpawnedHero);
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

        if (defaultHeroPrefab != null)
        {
            Debug.LogWarning($"{nameof(HeroGameplaySpawner)} has no prefab mapped for {characterClass}; using default hero prefab.", gameObject);
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

        if (characterClass != CharacterClass.Knight)
            return;

        const int defaultSlotCount = 4;
        PlayerSkillTreeManager skillTreeManager = PlayerSkillTreeManager.Service;
        skillTreeManager.EnsureLevelRewarded(PlayerExperienceStorage.Level);
        ApplySkillTreeStats(hero);

        IReadOnlyList<SkillTreeDefinition> loadoutTrees = GetLoadoutTrees();
        foreach (SkillTreeDefinition tree in loadoutTrees)
            skillTreeManager.EnsureUnlockedActiveSkillsEquipped(tree, defaultSlotCount);

        skillTreeManager.ApplyEquippedSkillsToHero(hero, loadoutTrees, defaultSlotCount);

        HeroSkillLoadoutPhotonSync loadoutSync = hero.GetComponent<HeroSkillLoadoutPhotonSync>();
        if (loadoutSync == null)
            loadoutSync = hero.gameObject.AddComponent<HeroSkillLoadoutPhotonSync>();

        loadoutSync.SetSkillTrees(skillTree, elementalSkillTree);
        loadoutSync.PublishLocalLoadout();
    }

    private void ApplySkillTreeStats(HeroCtrl hero)
    {
        if (hero == null || hero.CharacterStat == null)
            return;

        List<StatModifier> modifiers = new();
        foreach (SkillTreeDefinition tree in GetLoadoutTrees())
        {
            SkillTreeRuntime runtime = new(tree);
            modifiers.AddRange(runtime.CreateStatModifiers());
        }

        hero.CharacterStat.RecalculateSkillTree(modifiers);
    }

    private IReadOnlyList<SkillTreeDefinition> GetLoadoutTrees()
    {
        List<SkillTreeDefinition> trees = new();
        if (skillTree != null)
            trees.Add(skillTree);

        if (elementalSkillTree != null && !trees.Contains(elementalSkillTree))
            trees.Add(elementalSkillTree);

        return trees;
    }
}
