using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BossHealthBar : BaseMonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider delayedHealthSlider;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;

    private BossEnemy activeBoss;
    private CharacterDamReceiver activeReceiver;
    private float targetHealth;
    private float displayedDelayedHealth;
    protected override void OnEnable()
    {
        base.OnEnable();
        BossEnemy.OnBossSpawned += ShowBoss;
        BossEnemy.OnBossDefeated += HideBoss;
        FindAndShowExistingBoss();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        BossEnemy.OnBossSpawned -= ShowBoss;
        BossEnemy.OnBossDefeated -= HideBoss;
        UnbindReceiver();
    }

    protected override void Update()
    {
        base.Update();

        if (content == null || !content.activeSelf)
            return;

        if (activeBoss == null || !activeBoss.IsBoss || !activeBoss.gameObject.activeInHierarchy)
        {
            HideBoss(activeBoss);
            return;
        }

        displayedDelayedHealth = Mathf.MoveTowards(displayedDelayedHealth, targetHealth, Time.unscaledDeltaTime * 0.35f);
        delayedHealthSlider.value = displayedDelayedHealth;
    }
    protected override void LoadComponents()
    {
        base.LoadComponents();

        content ??= transform.Find("Content")?.gameObject;
        healthSlider ??= FindContentChild<Slider>("HealthSlider");
        delayedHealthSlider ??= FindContentChild<Slider>("DelayedDamageSlider");
        nameText ??= FindContentChild<TMP_Text>("BossName");
        healthText ??= FindContentChild<TMP_Text>("HealthValue");
    }

    private T FindContentChild<T>(string childName) where T : Component
    {
        return content != null
            ? content.transform.Find(childName)?.GetComponent<T>()
            : null;
    }

    private void ShowBoss(BossEnemy boss)
    {
        if (boss == null || !boss.IsBoss)
            return;

        LoadComponents();
        if (!HasViewReferences())
        {
            Debug.LogWarning($"{nameof(BossHealthBar)} is missing a required UI child.", this);
            return;
        }

        UnbindReceiver();
        activeBoss = boss;
        activeReceiver = boss.DamageReceiver ?? boss.GetComponentInChildren<CharacterDamReceiver>();
        if (activeReceiver != null)
            activeReceiver.OnHpChanged += RefreshHealth;

        nameText.text = boss.DisplayName.ToUpperInvariant();
        content.SetActive(true);

        CharacterStat stat = activeReceiver != null ? activeReceiver.CharacterCtrl?.CharacterStat : null;
        RefreshHealth(stat?.CurrentHealth ?? 0f, stat?.MaxHealth?.FinalValue ?? 1f);
    }

    private void HideBoss(BossEnemy boss)
    {
        if (boss != null && boss != activeBoss)
            return;

        UnbindReceiver();
        activeBoss = null;
        if (content != null)
            content.SetActive(false);
    }

    private void RefreshHealth(float current, float maximum)
    {
        float normalized = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        targetHealth = normalized;
        displayedDelayedHealth = Mathf.Max(displayedDelayedHealth, normalized);

        healthSlider.value = normalized;
        healthText.text = $"{Mathf.CeilToInt(current):N0} / {Mathf.CeilToInt(maximum):N0}";
    }

    private void UnbindReceiver()
    {
        if (activeReceiver != null)
            activeReceiver.OnHpChanged -= RefreshHealth;
        activeReceiver = null;
    }

    private void FindAndShowExistingBoss()
    {
        BossEnemy[] bosses = FindObjectsByType<BossEnemy>(FindObjectsInactive.Exclude);
        foreach (BossEnemy boss in bosses)
        {
            if (boss.IsBoss)
            {
                ShowBoss(boss);
                return;
            }
        }
    }
    private bool HasViewReferences()
    {
        return content != null && healthSlider != null && delayedHealthSlider != null && nameText != null && healthText != null;
    }
}
