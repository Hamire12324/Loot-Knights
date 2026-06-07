using UnityEngine;
using Unity.Cinemachine;

[DefaultExecutionOrder(-10000)]
public class HeroGameplaySpawner : BaseMonoBehaviour
{
    [SerializeField] private HeroCtrl defaultHeroPrefab;
    [SerializeField] private CharacterClassHeroPrefab[] classPrefabs;
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

        HeroCtrl heroPrefab = GetHeroPrefab(characterData.CharacterClass);
        Vector3 spawnPosition = GetSpawnPosition();

        if (destroySceneHeroesBeforeSpawn) DestroySceneHeroes();

        Transform parent = spawnedHeroParent != null ? spawnedHeroParent : null;
        SpawnedHero = Instantiate(heroPrefab, spawnPosition, Quaternion.identity, parent);
        SpawnedHero.ApplyProfile(characterData);
        PlayerEquipmentManager.InstanceOrNull?.ApplyToHero(SpawnedHero);
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
}
